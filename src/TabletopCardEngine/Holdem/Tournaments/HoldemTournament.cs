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
                return HoldemResult<TournamentData>.Ok(TournamentDataMapper.ToTournamentData(tournament!));
            case TournamentActionRegister register:
                error = Register(register, tournament!);
                break;
            case TournamentActionUnregister unregister:
                error = Unregister(unregister, tournament!);
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

        LogApplied(action, tournament!);
        SetSuccessTags(activity, actionType, start);
        return HoldemResult<TournamentData>.Ok(TournamentDataMapper.ToTournamentData(tournament!));
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
    private void LogApplied(TournamentAction action, Tournament tournament)
    {
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
