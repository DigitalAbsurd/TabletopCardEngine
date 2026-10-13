using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TabletopCardEngine.Data;
using TabletopCardEngine.Diagnostics;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.HoldemActionHandlers;
using TabletopCardEngine.Mappers;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem;

/// <summary>
/// The <see cref="IHoldemTable"/> the container registers: rehydrates the game, runs the handler, saves and projects.
/// </summary>
internal sealed class HoldemTable : IHoldemTable
{
    private readonly HoldemEngineOptions _options;
    private readonly ILogger<HoldemTable> _logger;
    private readonly HoldemEngineMetrics _metrics;
    private readonly HoldemSchemaManager _schemaManager;
    private volatile bool _schemaChecked;

    internal HoldemTable(HoldemEngineOptions options, ILogger<HoldemTable> logger, HoldemEngineMetrics metrics, HoldemSchemaManager schemaManager)
    {
        _options = options;
        _logger = logger;
        _metrics = metrics;
        _schemaManager = schemaManager;
        _schemaChecked = false;
    }

    /// <inheritdoc/>
    public Task<HoldemResult<GameData>> ApplyAsync(HoldemAction action, CancellationToken cancellationToken = default)
        => ApplyWithHookAsync(action, null, cancellationToken);

    /// <summary>The same call with a seam between handling the action and saving it, so a test can interleave two writers.</summary>
    internal async Task<HoldemResult<GameData>> ApplyWithHookAsync(HoldemAction holdemAction, Func<Task>? beforeSave, CancellationToken cancellationToken)
    {
        if (!_schemaChecked)
        {
            await _schemaManager.EnsureCompatibleAsync(cancellationToken);
            _schemaChecked = true;
        }

        long start = Stopwatch.GetTimestamp();
        using var activity = HoldemEngineActivity.Source.StartActivity(HoldemEngineActivity.Name, ActivityKind.Internal);

        var actionType = HoldemActionNames.Of(holdemAction);
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(HoldemEngineActivity.ActionTypeTag, actionType);
            activity.SetTag(HoldemEngineActivity.GameIdTag, holdemAction.GameId.ToString("D"));
        }

        try
        {
            return await ApplyCoreAsync(holdemAction, beforeSave, activity, actionType, start, cancellationToken);
        }
        catch (Exception ex) when (RecordException(activity, ex, actionType, start))
        {
            throw;
        }
    }

    /// <summary>Apply the action and set the outcome tag, guarding expensive tag computation.</summary>
    private async Task<HoldemResult<GameData>> ApplyCoreAsync(HoldemAction holdemAction, Func<Task>? beforeSave, Activity? activity, string actionType, long start, CancellationToken cancellationToken)
    {
        IHoldemActionHandler holdemActionHandler = IHoldemActionHandler.CreateHandlerForAction(holdemAction, _options);

        using var dbContext = await _options.DbContextFactory.CreateDbContextAsync(cancellationToken);

        var game = await dbContext.LoadGameWithCurrentHandAsync(holdemAction.GameId);

        if (activity is { IsAllDataRequested: true } && holdemAction is HoldemActionPlay playAction)
        {
            activity.SetTag(HoldemEngineActivity.PlayerActionTypeTag, playAction.ActionType.ToString());
        }

        var handBefore = game.GetCurrentHand();
        using var scope = _logger.BeginGameScope(holdemAction.GameId, handBefore.No);
        var before = new HandSnapshot(handBefore.No, game.IsCurrentHandEnded(), handBefore.GetStreet(), handBefore.GetDeckCards().Count);

        var existed = game.Id != default;
        if (!existed && holdemAction is not HoldemActionCreateGame)
        {
            _logger.ActionRejected(actionType, holdemAction.GameId, HoldemErrorCode.GameNotFound);
            SetOutcomeTagsAndReturn(activity, "Failure", HoldemErrorCode.GameNotFound, actionType, start);
            return HoldemResult<GameData>.Fail(new HoldemError(HoldemErrorCode.GameNotFound, PackResource.HoldemValidation_GameNotFound));
        }

        if (existed && holdemAction is not HoldemActionCreateGame && holdemAction.ExpectedVersion is { } expected && expected != game.Version)
        {
            _logger.ConcurrencyConflict(actionType, holdemAction.GameId, HoldemEngineLog.ExpectedVersion);
            SetOutcomeTagsAndReturn(activity, "Failure", HoldemErrorCode.Conflict, actionType, start);
            return HoldemResult<GameData>.Fail(ConflictError());
        }

        var error = await holdemActionHandler.HandleAsync(holdemAction, game, cancellationToken);
        if (error is not null)
        {
            _logger.ActionRejected(actionType, holdemAction.GameId, error.Code);
            SetOutcomeTagsAndReturn(activity, "Failure", error.Code, actionType, start);
            return HoldemResult<GameData>.Fail(error);
        }

        if (holdemAction is HoldemActionGetGame)
        {
            LogApplied(holdemAction, actionType, game, before);
            SetOutcomeTags(activity, game, "Success", actionType, start);
            return HoldemResult<GameData>.Ok(GameDataMapper.ToGameData(game));
        }

        if (existed)
        {
            game.Version++;
        }

        if (beforeSave is not null)
        {
            await beforeSave();
        }

        if (existed && await LostRaceBeforeSaveAsync(dbContext, game, cancellationToken))
        {
            return ConcurrentSaveConflict(holdemAction, activity, actionType, start);
        }

        try
        {
            if (existed && dbContext.Database.IsRelational())
            {
                if (!await SaveVersionFirstAsync(dbContext, game, cancellationToken))
                {
                    return ConcurrentSaveConflict(holdemAction, activity, actionType, start);
                }
            }
            else
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrentSaveConflict(holdemAction, activity, actionType, start);
        }

        LogApplied(holdemAction, actionType, game, before);
        LogSensitiveCards(game, before);

        SetOutcomeTags(activity, game, "Success", actionType, start);
        return HoldemResult<GameData>.Ok(GameDataMapper.ToGameData(game));
    }

    /// <summary>
    /// The <c>Game.Version</c> check, run ahead of the save on a non-relational provider. A relational database
    /// checks the token in the <c>UPDATE Game</c> statement, inside the transaction that also inserts the new
    /// rows. EF's InMemory provider has no transaction and applies every insert before any update, so a lost
    /// race there fails on a duplicate <see cref="HandAction"/> key, with an exception that names no conflict,
    /// before the token is ever compared. This narrows the window rather than closing it, which is all a
    /// store without transactions allows. It only stands in for the token, so it is skipped when
    /// <c>Version</c> is not one.
    /// </summary>
    private static async Task<bool> LostRaceBeforeSaveAsync(HoldemDbContext dbContext, Game game, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            return false;
        }

        var version = dbContext.Entry(game).Property(g => g.Version);
        if (!version.Metadata.IsConcurrencyToken)
        {
            return false;
        }

        var storedVersion = await dbContext.Set<Game>()
            .AsNoTracking()
            .Where(g => g.Id == game.Id)
            .Select(g => g.Version)
            .SingleAsync(cancellationToken);
        return storedVersion != version.OriginalValue;
    }

    /// <summary>
    /// The save on a relational database, with the <c>Game.Version</c> check run as a statement of its own before
    /// any child row is written. A provider that batches the save (Npgsql, SqlClient) sends the token check and
    /// the new <see cref="HandAction"/> inserts together, so a lost race used to fail on a duplicate key, with an
    /// exception that names no conflict, before EF looked at the <c>UPDATE Game</c> row count. Here the
    /// conditional update runs first inside one transaction: a writer that lost the race updates no row and
    /// rolls back with nothing written, and one that is still racing waits on the row's write lock, which the
    /// update takes anyway, and then sees the new version. The rest of the change set is saved after it, in the
    /// same transaction, against the version this call just wrote.
    /// </summary>
    /// <returns><see langword="false"/> when another writer saved the game first; nothing is persisted then.</returns>
    private static Task<bool> SaveVersionFirstAsync(HoldemDbContext dbContext, Game game, CancellationToken cancellationToken)
    {
        var version = dbContext.Entry(game).Property(g => g.Version);
        var loadedVersion = version.OriginalValue;
        var newVersion = version.CurrentValue;

        // A user-initiated transaction has to run inside the execution strategy, or a consumer's
        // EnableRetryOnFailure rejects it. A retry repeats the whole unit, which is why both versions are read
        // before the first attempt rather than from the entry the attempt changes.
        return dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            var updated = await dbContext.Set<Game>()
                .Where(g => g.Id == game.Id && g.Version == loadedVersion)
                .ExecuteUpdateAsync(setters => setters.SetProperty(g => g.Version, newVersion), ct);
            if (updated == 0)
            {
                return false;
            }

            // The row already holds the new version, so the change set's own token check is made against it.
            version.OriginalValue = newVersion;
            await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, ct);
            await transaction.CommitAsync(ct);
            dbContext.ChangeTracker.AcceptAllChanges();
            return true;
        }, cancellationToken);
    }

    /// <summary>Reports that another writer saved the game between this call's load and its save.</summary>
    private HoldemResult<GameData> ConcurrentSaveConflict(HoldemAction holdemAction, Activity? activity, string actionType, long start)
    {
        _logger.ConcurrencyConflict(actionType, holdemAction.GameId, HoldemEngineLog.ConcurrentSave);
        SetOutcomeTagsAndReturn(activity, "Failure", HoldemErrorCode.Conflict, actionType, start);
        return HoldemResult<GameData>.Fail(ConflictError());
    }

    /// <summary>Record an exception in the activity and record metrics, then return false to satisfy the exception filter's pattern.</summary>
    private bool RecordException(Activity? activity, Exception ex, string actionType, long start)
    {
        if (activity is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            activity.AddException(ex);
            activity.SetTag(HoldemEngineActivity.OutcomeTag, "Exception");
        }

        _metrics.RecordAction(actionType, "Exception");
        _metrics.RecordActionDuration(actionType, "Exception", Stopwatch.GetElapsedTime(start).TotalSeconds);

        return false;
    }

    /// <summary>Set outcome, error code (if a failure), and hand-related tags at return time, and record metrics.</summary>
    private void SetOutcomeTagsAndReturn(Activity? activity, string outcome, HoldemErrorCode errorCode, string actionType, long start)
    {
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(HoldemEngineActivity.OutcomeTag, outcome);
            activity.SetTag(HoldemEngineActivity.ErrorCodeTag, errorCode.ToString());
        }

        _metrics.RecordAction(actionType, outcome);
        _metrics.RecordValidationFailure(actionType, errorCode);
        _metrics.RecordActionDuration(actionType, outcome, Stopwatch.GetElapsedTime(start).TotalSeconds);
    }

    /// <summary>Set outcome, hand.no, and street tags at return time, and record metrics.</summary>
    private void SetOutcomeTags(Activity? activity, Game game, string outcome, string actionType, long start)
    {
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(HoldemEngineActivity.OutcomeTag, outcome);

            var hand = game.GetCurrentHand();
            if (hand.No != 0)
            {
                activity.SetTag(HoldemEngineActivity.HandNumberTag, hand.No);
            }

            activity.SetTag(HoldemEngineActivity.StreetTag, hand.GetStreet().ToString());
        }

        _metrics.RecordAction(actionType, outcome);
        _metrics.RecordActionDuration(actionType, outcome, Stopwatch.GetElapsedTime(start).TotalSeconds);
    }

    /// <summary>What the current hand looked like before the handler ran, to tell what the call changed.</summary>
    private readonly record struct HandSnapshot(int No, bool Ended, Street Street, int DeckCardCount);

    /// <summary>Logs the lifecycle events a successful call caused, then the call itself. Scalars only: never the game.</summary>
    private void LogApplied(HoldemAction holdemAction, string actionType, Game game, HandSnapshot before)
    {
        var gameId = holdemAction.GameId;
        if (holdemAction is HoldemActionCreateGame)
        {
            _logger.GameCreated(gameId, game.MaxPlayersCount, game.InitialSmallBlind);
        }

        var hand = game.GetCurrentHand();
        var sameHand = hand.No == before.No;
        if (hand.No > before.No)
        {
            _logger.HandStarted(hand.No, gameId, hand.PlayerStartStates.Count);
            _metrics.RecordHandStarted();
        }

        var ended = game.IsCurrentHandEnded();
        if (ended && !(sameHand && before.Ended))
        {
            _logger.HandFinished(hand.No, gameId);
        }
        else if (sameHand && !ended)
        {
            var street = hand.GetStreet();
            if (street != before.Street)
            {
                _logger.StreetDealt(hand.No, gameId, street);
            }
        }

        if (holdemAction is HoldemActionFinishGame)
        {
            _logger.GameFinished(gameId);
        }

        _logger.ActionApplied(actionType, gameId, game.Version);
    }

    /// <summary>Logs hole cards and board cards when enabled.</summary>
    private void LogSensitiveCards(Game game, HandSnapshot before)
    {
        if (!(_options.Settings.EnableSensitiveCardLogging && _logger.IsEnabled(LogLevel.Trace)))
        {
            return;
        }

        var hand = game.GetCurrentHand();
        var gameId = game.Id;
        var sameHand = hand.No == before.No;

        if (hand.No > before.No)
        {
            foreach (var playerStartState in hand.PlayerStartStates.OrderBy(p => p.SeatNo))
            {
                var holeCards = hand.GetSeatHoleCards(playerStartState.SeatNo);
                if (holeCards is not null && holeCards.Length > 0)
                {
                    _logger.HoleCardsDealt(hand.No, gameId, playerStartState.SeatNo, holeCards);
                }
            }
        }

        if (sameHand)
        {
            var deckCards = hand.GetDeckCards();
            var finalDeckCount = deckCards.Count;

            if (finalDeckCount > before.DeckCardCount)
            {
                if (finalDeckCount >= 3 && before.DeckCardCount < 3)
                {
                    var flopCards = deckCards.Skip(0).Take(3);
                    _logger.BoardDealt(hand.No, gameId, Street.Flop, flopCards);
                }

                if (finalDeckCount >= 4 && before.DeckCardCount < 4)
                {
                    var turnCard = deckCards.Skip(3).Take(1);
                    _logger.BoardDealt(hand.No, gameId, Street.Turn, turnCard);
                }

                if (finalDeckCount >= 5 && before.DeckCardCount < 5)
                {
                    var riverCard = deckCards.Skip(4).Take(1);
                    _logger.BoardDealt(hand.No, gameId, Street.River, riverCard);
                }
            }
        }
    }

    private static HoldemError ConflictError()
        => new(HoldemErrorCode.Conflict, PackResource.HoldemValidation_Conflict);
}
