using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Diagnostics;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.HoldemActionHandlers;
using OpenHoldemEngine.Mappers;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Resources;
using System.Diagnostics;

namespace OpenHoldemEngine;

/// <summary>
/// The single entry point for game commands: every game action goes through <see cref="ApplyHoldemActionAsync"/>.
/// There is no long-lived in-memory game: every call rehydrates the aggregate from the database, applies one action,
/// and projects the result back out.
/// <para>
/// <b>Scope of v1:</b> No-Limit Texas Hold'em cash games at 2 to 6 seats, with no antes and no rake.
/// Other structures (Pot-Limit, Fixed-Limit) are v2, and <see cref="GameOptionData"/> is where they will plug in.
/// </para>
/// <para>
/// Instances are obtained from the dependency-injection container registered by
/// <c>AddHoldemEngine</c>: <c>services.GetRequiredService&lt;HoldemEngine&gt;()</c>.
/// </para>
/// </summary>
public sealed class HoldemEngine
{
    private readonly HoldemEngineOptions _options;
    private readonly ILogger<HoldemEngine> _logger;
    private readonly HoldemEngineMetrics _metrics;
    private readonly HoldemSchemaManager _schemaManager;
    private volatile bool _schemaChecked;

    internal HoldemEngine(HoldemEngineOptions options, ILogger<HoldemEngine> logger, HoldemEngineMetrics metrics, HoldemSchemaManager schemaManager)
    {
        _options = options;
        _logger = logger;
        _metrics = metrics;
        _schemaManager = schemaManager;
        _schemaChecked = false;
    }

    /// <summary>
    /// Applies one action to a game and returns the game as it now stands.
    /// <para>
    /// A failed result short-circuits before the transaction is saved, so a validation failure
    /// leaves nothing persisted.
    /// </para>
    /// <para>
    /// <b>Concurrency:</b> <see cref="GameData.Version"/> counts the saves of a game, starting at <c>1</c>
    /// on creation; every call that saves increments it, and reading the game does not. The call fails with
    /// <see cref="HoldemErrorCode.Conflict"/> when <see cref="HoldemAction.ExpectedVersion"/> is set and no
    /// longer matches, or when another writer saved the game between this call's load and its save. Nothing
    /// is persisted on a conflict, and the engine never retries: the caller reloads with
    /// <see cref="HoldemAction.GetGame(Guid)"/> and decides.
    /// </para>
    /// </summary>
    /// <param name="holdemAction">
    /// The action to apply. Its concrete record type decides which handler runs.
    /// </param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>
    /// The projected game on success, or a failed result carrying the validation errors, each with a stable <see cref="HoldemErrorCode"/>.
    /// </returns>
    public Task<HoldemResult<GameData>> ApplyHoldemActionAsync(HoldemAction holdemAction, CancellationToken cancellationToken = default)
        => ApplyWithHookAsync(holdemAction, null, cancellationToken);

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

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.ConcurrencyConflict(actionType, holdemAction.GameId, HoldemEngineLog.ConcurrentSave);
            SetOutcomeTagsAndReturn(activity, "Failure", HoldemErrorCode.Conflict, actionType, start);
            return HoldemResult<GameData>.Fail(ConflictError());
        }

        LogApplied(holdemAction, actionType, game, before);
        LogSensitiveCards(game, before);

        SetOutcomeTags(activity, game, "Success", actionType, start);
        return HoldemResult<GameData>.Ok(GameDataMapper.ToGameData(game));
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
        else if (sameHand && !ended && hand.GetStreet() != before.Street)
        {
            _logger.StreetDealt(hand.No, gameId, hand.GetStreet());
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
