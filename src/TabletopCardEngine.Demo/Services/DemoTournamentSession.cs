using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// The demo's tournament: the one place that calls <see cref="IHoldemTournament"/>. It knows the tables, which
/// of them the visitor is looking at, and deals a table's next hand through the tournament instead of
/// <c>StartHand</c>. A table is an ordinary game, so showing one and acting at it go through
/// <see cref="DemoGameSession"/>, whose current game is always the viewed table.
/// </summary>
/// <param name="tournament">The engine's tournament entry point, from the play engine's container.</param>
/// <param name="session">The session whose current game is the table on screen.</param>
public sealed class DemoTournamentSession(IHoldemTournament tournament, DemoGameSession session)
{
    private readonly Random _seeds = new();

    /// <summary>Gets the tournament as the engine last returned it, or <c>null</c> when none has been created.</summary>
    public TournamentData? Tournament { get; private set; }

    /// <summary>Gets whether a tournament has been created in this session, finished or not.</summary>
    public bool IsActive => Tournament is not null;

    /// <summary>Gets whether the tournament is running: started and not yet down to one player.</summary>
    public bool IsRunning => Tournament?.State == TournamentState.Running;

    /// <summary>Gets whether the tournament has finished.</summary>
    public bool IsFinished => Tournament?.State == TournamentState.Finished;

    /// <summary>Gets the seed the seat draw used, to start the same draw again; <c>null</c> before the start.</summary>
    public int? SeatDrawSeed { get; private set; }

    /// <summary>Gets the number of the table the visitor is looking at; <c>0</c> when none.</summary>
    public int ViewedTableNo { get; private set; }

    /// <summary>Gets the table the visitor is looking at, or <c>null</c> before the start.</summary>
    public TournamentTableData? ViewedTable => Tournament?.Tables.FirstOrDefault(table => table.TableNo == ViewedTableNo);

    /// <summary>Gets the viewed table's game, as read through <see cref="IHoldemTable"/>; <c>null</c> before the start.</summary>
    public GameData? ViewedGame => IsActive ? session.CurrentGame : null;

    /// <summary>Gets the tables that still deal hands: every table that has not been broken up.</summary>
    public IReadOnlyList<TournamentTableData> OpenTables
        => Tournament?.Tables.Where(table => !table.IsBroken).ToArray() ?? [];

    /// <summary>Gets the winner's name once the tournament has finished, otherwise <c>null</c>.</summary>
    public string? WinnerName
        => IsFinished ? Tournament!.Entries.FirstOrDefault(entry => entry.Place == 1)?.Name : null;

    /// <summary>Raised whenever the tournament or the viewed table changes after a successful call.</summary>
    public event EventHandler<EventArgs>? StateChanged;

    /// <summary>Raised whenever a call fails, carrying the engine's message.</summary>
    public event EventHandler<EngineErrorEventArgs>? EngineError;

    /// <summary>Creates a tournament with the given rules; it is registering until <see cref="StartAsync"/>.</summary>
    /// <param name="options">The rules of the tournament.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if it was created, <c>false</c> on validation error.</returns>
    public async Task<bool> CreateAsync(TournamentOptionData options, CancellationToken cancellationToken = default)
    {
        Forget();
        return await ApplyAsync(TournamentAction.CreateTournament(options), cancellationToken);
    }

    /// <summary>Registers one player for each name, in order, stopping at the first the engine refuses.</summary>
    /// <param name="names">The players' display names.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if every name was registered, <c>false</c> on the first error.</returns>
    public async Task<bool> RegisterAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (!RequireTournament())
        {
            return false;
        }

        foreach (var name in names)
        {
            if (!await ApplyAsync(TournamentAction.Register(Tournament!.TournamentId, name), cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Closes registration, draws the seats and deals the first hands, then shows the first table.</summary>
    /// <param name="seatDrawSeed">The seed of the draw, or <c>null</c> to pick a random one; either way it is kept in <see cref="SeatDrawSeed"/>.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the tournament started, <c>false</c> on validation error.</returns>
    public async Task<bool> StartAsync(int? seatDrawSeed = null, CancellationToken cancellationToken = default)
    {
        if (!RequireTournament())
        {
            return false;
        }

        var seed = seatDrawSeed ?? _seeds.Next();
        if (!await ApplyAsync(TournamentAction.StartTournament(Tournament!.TournamentId, seed), cancellationToken))
        {
            return false;
        }

        SeatDrawSeed = seed;
        return await ViewTableAsync(Tournament!.Tables[0].TableNo, cancellationToken);
    }

    /// <summary>Creates the tournament, registers the names and starts it: what the form does in one go.</summary>
    /// <param name="options">The rules of the tournament.</param>
    /// <param name="names">The players' display names.</param>
    /// <param name="seatDrawSeed">The seed of the seat draw, or <c>null</c> for a random one.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the tournament is running, <c>false</c> on the first error.</returns>
    public async Task<bool> CreateAndStartAsync(
        TournamentOptionData options,
        IEnumerable<string> names,
        int? seatDrawSeed = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateAsync(options, cancellationToken)
            && await RegisterAsync(names, cancellationToken)
            && await StartAsync(seatDrawSeed, cancellationToken);
    }

    /// <summary>Shows another table: reads its game back, so every action that follows goes to it.</summary>
    /// <param name="tableNo">The table to show.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the table is shown, <c>false</c> when the tournament has no such table.</returns>
    public async Task<bool> ViewTableAsync(int tableNo, CancellationToken cancellationToken = default)
    {
        if (!RequireTournament())
        {
            return false;
        }

        var table = Tournament!.Tables.FirstOrDefault(candidate => candidate.TableNo == tableNo);
        if (table is null)
        {
            RaiseEngineError($"The tournament has no table {tableNo}.");
            return false;
        }

        ViewedTableNo = tableNo;
        var viewed = await session.ViewGameAsync(table.GameId, cancellationToken);
        RaiseStateChanged();
        return viewed;
    }

    /// <summary>
    /// Deals a table's next hand through the tournament, which first records who the last hand eliminated. The
    /// viewed table is shown again afterwards, or the first table still open when the viewed one was broken up.
    /// </summary>
    /// <param name="tableNo">The table to deal.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the call succeeded, <c>false</c> on validation error.</returns>
    public async Task<bool> DealTableAsync(int tableNo, CancellationToken cancellationToken = default)
    {
        if (!RequireTournament())
        {
            return false;
        }

        if (!await ApplyAsync(TournamentAction.DealTable(Tournament!.TournamentId, tableNo), cancellationToken))
        {
            return false;
        }

        var open = OpenTables;
        var shownTableNo = open.Any(table => table.TableNo == ViewedTableNo) || open.Count == 0
            ? ViewedTableNo
            : open[0].TableNo;
        return await ViewTableAsync(shownTableNo, cancellationToken);
    }

    /// <summary>Forgets the tournament, which a new game or a new tournament replaces.</summary>
    public void Reset()
    {
        Forget();
        RaiseStateChanged();
    }

    private void Forget()
    {
        Tournament = null;
        SeatDrawSeed = null;
        ViewedTableNo = 0;
    }

    private bool RequireTournament()
    {
        if (Tournament is not null)
        {
            return true;
        }

        RaiseEngineError("No tournament has been created yet.");
        return false;
    }

    private async Task<bool> ApplyAsync(TournamentAction action, CancellationToken cancellationToken)
    {
        try
        {
            var result = await tournament.ApplyAsync(action, cancellationToken);
            if (result.IsSuccess)
            {
                Tournament = result.Value;
                RaiseStateChanged();
                return true;
            }

            RaiseEngineError(result.Errors.Select(error => error.Message).FirstOrDefault() ?? "An unknown error occurred.");
            return false;
        }
        catch (Exception ex)
        {
            // Engine or database exceptions are shown as an engine error, as DemoGameSession does.
            RaiseEngineError($"Engine call failed: {ex.Message}");
            return false;
        }
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseEngineError(string message)
    {
        EngineError?.Invoke(this, new EngineErrorEventArgs(message));
    }
}
