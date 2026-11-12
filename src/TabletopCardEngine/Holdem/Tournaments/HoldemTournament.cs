using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Diagnostics;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// The <see cref="IHoldemTournament"/> the container registers: reloads the tournament, applies the action, saves and
/// projects, the way <see cref="HoldemTable"/> does for a game.
/// </summary>
internal sealed class HoldemTournament : IHoldemTournament
{
    private readonly HoldemOptions _options;
    private readonly ILogger<HoldemTournament> _logger;
    private readonly HoldemMetrics _metrics;
    private readonly HoldemSchemaManager _schemaManager;
    private volatile bool _schemaChecked;

    internal HoldemTournament(HoldemOptions options, ILogger<HoldemTournament> logger, HoldemMetrics metrics, HoldemSchemaManager schemaManager)
    {
        _options = options;
        _logger = logger;
        _metrics = metrics;
        _schemaManager = schemaManager;
        _schemaChecked = false;
    }

    /// <inheritdoc/>
    public async Task<HoldemResult<TournamentData>> ApplyAsync(TournamentAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!_schemaChecked)
        {
            await _schemaManager.EnsureCompatibleAsync(cancellationToken);
            _schemaChecked = true;
        }

        long start = Stopwatch.GetTimestamp();
        using var activity = HoldemActivity.Source.StartActivity(HoldemActivity.TournamentName, ActivityKind.Internal);

        var actionType = NameOf(action);
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(HoldemActivity.ActionTypeTag, actionType);
            activity.SetTag(HoldemActivity.TournamentIdTag, action.TournamentId.ToString("D"));
        }

        try
        {
            return await ApplyCoreAsync(action, activity, actionType, start, cancellationToken);
        }
        catch (Exception ex) when (RecordException(activity, ex, actionType, start))
        {
            throw;
        }
    }

    /// <summary>The verb of a command, for logs, traces and metrics: <c>Unknown</c> for a type the engine does not know.</summary>
    internal static string NameOf(TournamentAction action) => action switch
    {
        TournamentActionCreate => "CreateTournament",
        TournamentActionGet => "GetTournament",
        TournamentActionRegister => "Register",
        TournamentActionUnregister => "Unregister",
        TournamentActionStart => "StartTournament",
        TournamentActionDealTable => "DealTable",
        _ => "Unknown"
    };

    private async Task<HoldemResult<TournamentData>> ApplyCoreAsync(TournamentAction action, Activity? activity, string actionType, long start, CancellationToken cancellationToken)
    {
        using var dbContext = await _options.DbContextFactory.CreateDbContextAsync(cancellationToken);

        var tournament = await dbContext.Set<Tournament>()
            .Include(candidate => candidate.Entries)
            .Include(candidate => candidate.Tables)
            .SingleOrDefaultAsync(candidate => candidate.Id == action.TournamentId, cancellationToken);

        var existed = tournament is not null;
        if (!existed && action is not TournamentActionCreate)
        {
            return Failure(activity, actionType, start, new HoldemError(HoldemErrorCode.TournamentNotFound, PackResource.HoldemValidation_TournamentNotFound));
        }

        if (existed && action is not TournamentActionCreate && action.ExpectedVersion is { } expected && expected != tournament!.Version)
        {
            return Failure(activity, actionType, start, ConflictError());
        }

        HoldemError? error;
        IReadOnlyList<TournamentStarter.OpenedTable> opened = [];
        var dealOutcome = TournamentDealer.Outcome.None;
        switch (action)
        {
            case TournamentActionCreate create:
                (tournament, error) = Create(create, tournament);
                if (tournament is not null)
                {
                    dbContext.Add(tournament);
                }

                break;
            case TournamentActionGet:
                SetSuccessTags(activity, actionType, start);
                return HoldemResult<TournamentData>.Ok(TournamentDataMapper.ToTournamentData(tournament!, _options.TimeProvider.GetUtcNow()));
            case TournamentActionRegister register:
                error = Register(register, tournament!);
                break;
            case TournamentActionUnregister unregister:
                error = Unregister(unregister, tournament!);
                break;
            case TournamentActionStart startTournament:
                (error, opened) = await TournamentStarter.StartAsync(startTournament, tournament!, dbContext, _options, cancellationToken);
                break;
            case TournamentActionDealTable dealTable:
                (error, dealOutcome) = await TournamentDealer.DealAsync(dealTable, tournament!, dbContext, _options, cancellationToken);
                opened = dealOutcome.Dealt is { } dealt ? [dealt] : [];
                break;
            default:
                error = new HoldemError(HoldemErrorCode.InvalidAction, PackResource.HoldemValidation_TournamentInvalidAction);
                break;
        }

        if (error is not null)
        {
            return Failure(activity, actionType, start, error);
        }

        if (existed)
        {
            tournament!.Version++;
        }

        if (!await VersionedSave.TrySaveAsync(dbContext, tournament!, existed, cancellationToken))
        {
            return Failure(activity, actionType, start, ConflictError());
        }

        LogApplied(action, tournament!, opened, dealOutcome);
        SetSuccessTags(activity, actionType, start);
        return HoldemResult<TournamentData>.Ok(TournamentDataMapper.ToTournamentData(tournament!, _options.TimeProvider.GetUtcNow()));
    }

    private static (Tournament? Tournament, HoldemError? Error) Create(TournamentActionCreate action, Tournament? existing)
    {
        if (existing is not null)
        {
            return (null, new HoldemError(HoldemErrorCode.TournamentAlreadyExists, PackResource.HoldemValidation_TournamentAlreadyExists));
        }

        var options = action.Options;
        var optionsError = TournamentOptionsValidation.Validate(options);
        if (optionsError is not null)
        {
            return (null, optionsError);
        }

        return (new Tournament
        {
            Id = action.TournamentId,
            Version = 1,
            State = (byte)TournamentState.Registering,
            BettingStructure = options.BettingStructure,
            TableFormat = options.TableFormat,
            MaxPlayersPerTable = options.MaxPlayersPerTable,
            RaiseCap = options.RaiseCap,
            AnteType = options.AnteType,
            StartingStack = options.StartingStack,
            BuyIn = options.BuyIn,
            MinEntries = options.MinEntries,
            MaxEntries = options.MaxEntries,
            Levels = TournamentDataMapper.ToEntityLevels(options.Levels),
            Payouts = [.. options.Payouts],
        }, null);
    }

    private static HoldemError? Register(TournamentActionRegister action, Tournament tournament)
    {
        if (tournament.State != (byte)TournamentState.Registering)
        {
            return new HoldemError(HoldemErrorCode.TournamentAlreadyStarted, PackResource.HoldemValidation_TournamentAlreadyStarted);
        }

        var active = tournament.ActiveEntries().ToList();
        if (active.Count >= tournament.MaxEntries)
        {
            return new HoldemError(HoldemErrorCode.TournamentFull, PackResource.HoldemValidation_TournamentFull);
        }

        var name = action.Name;
        if (string.IsNullOrWhiteSpace(name)
            || name.Length > AmountRules.PlayerNameMaxLength
            || active.Any(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return new HoldemError(HoldemErrorCode.InvalidEntry, PackResource.HoldemValidation_TournamentInvalidEntryName);
        }

        // Withdrawn rows count: the number of a withdrawn entry is not given again.
        var entryNo = tournament.Entries.Count == 0 ? 1 : tournament.Entries.Max(entry => entry.EntryNo) + 1;
        tournament.Entries.Add(new TournamentEntry { TournamentId = tournament.Id, EntryNo = entryNo, Name = name });
        return null;
    }

    private static HoldemError? Unregister(TournamentActionUnregister action, Tournament tournament)
    {
        if (tournament.State != (byte)TournamentState.Registering)
        {
            return new HoldemError(HoldemErrorCode.TournamentAlreadyStarted, PackResource.HoldemValidation_TournamentAlreadyStarted);
        }

        var entry = tournament.ActiveEntries().SingleOrDefault(candidate => candidate.EntryNo == action.EntryNo);
        if (entry is null)
        {
            return new HoldemError(HoldemErrorCode.InvalidEntry, PackResource.HoldemValidation_TournamentUnknownEntry);
        }

        entry.Place = TournamentDataMapper.WithdrawnPlace;
        return null;
    }

    /// <summary>Logs the lifecycle event a successful call caused. Scalars only: never a player's name.</summary>
    private void LogApplied(TournamentAction action, Tournament tournament, IReadOnlyList<TournamentStarter.OpenedTable> opened, TournamentDealer.Outcome dealOutcome)
    {
        // A table's first hand at a level is a Debug event, so it is checked before the Information gate below.
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var (tableNo, game) in opened)
            {
                var hand = game.GetCurrentHand();
                if (hand.No != 0 && hand.BlindLevelStartHandNo == hand.No)
                {
                    _logger.TournamentLevelStarted(tableNo, tournament.Id, hand.No, game.Id, hand.BlindLevel, hand.SmallBlind ?? game.InitialSmallBlind);
                }
            }
        }

        // A reserved seat found taken is a Warning, so it is checked before the Information gate below.
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            foreach (var entry in dealOutcome.SeatTaken)
            {
                _logger.TournamentEntrySeatTaken(entry.EntryNo, tournament.Id, entry.TableNo!.Value, entry.SeatNo!.Value);
            }
        }

        if (!_logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        switch (action)
        {
            case TournamentActionCreate:
                _logger.TournamentCreated(tournament.Id, tournament.Levels.Count, tournament.StartingStack);
                break;
            case TournamentActionRegister:
                _logger.TournamentEntryRegistered(tournament.Entries.Max(entry => entry.EntryNo), tournament.Id, tournament.ActiveEntries().Count());
                break;
            case TournamentActionUnregister unregister:
                _logger.TournamentEntryUnregistered(unregister.EntryNo, tournament.Id, tournament.ActiveEntries().Count());
                break;
            case TournamentActionStart:
                _logger.TournamentStarted(tournament.Id, tournament.ActiveEntries().Count(), tournament.Tables.Count);
                break;
            case TournamentActionDealTable:
                foreach (var entry in dealOutcome.Eliminated)
                {
                    _logger.TournamentEntryEliminated(entry.EntryNo, tournament.Id, entry.Place!.Value, entry.Prize, entry.EliminatedAtTableNo!.Value, entry.EliminatedInHandNo!.Value);
                }

                foreach (var move in dealOutcome.Moved)
                {
                    _logger.TournamentEntryMoved(move.EntryNo, tournament.Id, move.FromTableNo, move.ToTableNo, move.SeatNo, move.Stack);
                }

                if (dealOutcome.BrokenTableNo is { } brokenTableNo)
                {
                    _logger.TournamentTableBroken(brokenTableNo, tournament.Id, dealOutcome.Moved.Count, tournament.Tables.Count(table => !table.IsBroken));
                }

                if (dealOutcome.Winner is { } winner)
                {
                    _logger.TournamentFinished(tournament.Id, winner.EntryNo, winner.Prize);
                }

                break;
        }
    }

    private HoldemResult<TournamentData> Failure(Activity? activity, string actionType, long start, HoldemError error)
    {
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(HoldemActivity.OutcomeTag, "Failure");
            activity.SetTag(HoldemActivity.ErrorCodeTag, error.Code.ToString());
        }

        _metrics.RecordAction(actionType, "Failure");
        _metrics.RecordValidationFailure(actionType, error.Code);
        _metrics.RecordActionDuration(actionType, "Failure", Stopwatch.GetElapsedTime(start).TotalSeconds);
        return HoldemResult<TournamentData>.Fail(error);
    }

    private void SetSuccessTags(Activity? activity, string actionType, long start)
    {
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(HoldemActivity.OutcomeTag, "Success");
        }

        _metrics.RecordAction(actionType, "Success");
        _metrics.RecordActionDuration(actionType, "Success", Stopwatch.GetElapsedTime(start).TotalSeconds);
    }

    /// <summary>Record an exception in the activity and the metrics, then return false so the exception filter rethrows.</summary>
    private bool RecordException(Activity? activity, Exception ex, string actionType, long start)
    {
        if (activity is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            activity.AddException(ex);
            activity.SetTag(HoldemActivity.OutcomeTag, "Exception");
        }

        _metrics.RecordAction(actionType, "Exception");
        _metrics.RecordActionDuration(actionType, "Exception", Stopwatch.GetElapsedTime(start).TotalSeconds);

        return false;
    }

    private static HoldemError ConflictError()
        => new(HoldemErrorCode.Conflict, PackResource.HoldemValidation_TournamentConflict);
}
