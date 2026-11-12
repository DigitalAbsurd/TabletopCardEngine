namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// The one command type <see cref="IHoldemTournament"/> accepts. Which concrete record you pass decides what happens.
/// Every command is a <c>TournamentAction&lt;Verb&gt;</c> record with one static factory on this type. The shape
/// rule for the records: required data is positional, optional data is an <see langword="init"/> property.
/// </summary>
/// <param name="TournamentId">The tournament the action applies to.</param>
public abstract record TournamentAction(Guid TournamentId)
{
    /// <summary>
    /// The <see cref="TournamentData.Version"/> the caller last saw, or <see langword="null"/> to skip the check.
    /// When set and different from the tournament's current version, the call fails with
    /// <see cref="HoldemErrorCode.Conflict"/> before anything runs. Ignored by <see cref="TournamentActionCreate"/>,
    /// which has no tournament to compare against.
    /// </summary>
    public int? ExpectedVersion { get; init; }

    /// <summary>Creates a tournament with the given rules, generating an identifier when none is supplied.</summary>
    /// <param name="options">The rules the tournament is created with.</param>
    /// <param name="tournamentId">Identifier to use, or <see langword="null"/> to generate one.</param>
    public static TournamentActionCreate CreateTournament(TournamentOptionData options, Guid? tournamentId = null)
        => new(tournamentId ?? Guid.NewGuid(), options);

    /// <summary>Reads the tournament back without changing anything.</summary>
    /// <param name="tournamentId">The tournament to read.</param>
    public static TournamentActionGet GetTournament(Guid tournamentId)
        => new(tournamentId);

    /// <summary>Registers a player for a tournament that has not started yet.</summary>
    /// <param name="tournamentId">The tournament to register for.</param>
    /// <param name="name">The player's display name, 1 to 64 characters, unique within the tournament regardless of case.</param>
    public static TournamentActionRegister Register(Guid tournamentId, string name)
        => new(tournamentId, name);

    /// <summary>Withdraws a registration from a tournament that has not started yet.</summary>
    /// <param name="tournamentId">The tournament to withdraw from.</param>
    /// <param name="entryNo">The <see cref="TournamentEntryData.EntryNo"/> the registration was given.</param>
    public static TournamentActionUnregister Unregister(Guid tournamentId, int entryNo)
        => new(tournamentId, entryNo);

    /// <summary>Closes registration, draws the seats, opens the tables and deals their first hands.</summary>
    /// <param name="tournamentId">The tournament to start.</param>
    /// <param name="seatDrawSeed">The seed of the seat draw, to reproduce one; <see langword="null"/> to draw one at random.</param>
    public static TournamentActionStart StartTournament(Guid tournamentId, int? seatDrawSeed = null)
        => new(tournamentId) { SeatDrawSeed = seatDrawSeed };

    /// <summary>Records a table's eliminations after its hand has ended, and deals its next hand at the current level.</summary>
    /// <param name="tournamentId">The tournament the table belongs to.</param>
    /// <param name="tableNo">The <see cref="TournamentTableData.TableNo"/> of the table to deal.</param>
    public static TournamentActionDealTable DealTable(Guid tournamentId, int tableNo)
        => new(tournamentId, tableNo);
}

/// <summary>
/// Creates a tournament in the <see cref="TournamentState.Registering"/> state, at version <c>1</c>. Fails with
/// <see cref="HoldemErrorCode.TournamentAlreadyExists"/> if one with this identifier exists, with
/// <see cref="HoldemErrorCode.UnsupportedGameOptions"/> when the betting structure is not one the engine plays, and
/// with <see cref="HoldemErrorCode.InvalidTournamentOptions"/> when an option is out of range (see
/// <see cref="TournamentOptionData"/>).
/// </summary>
/// <param name="TournamentId">Identifier for the new tournament.</param>
/// <param name="Options">The rules the tournament is created with.</param>
public record TournamentActionCreate(Guid TournamentId, TournamentOptionData Options) : TournamentAction(TournamentId);

/// <summary>Reads the tournament back without changing anything; its version stays the same.</summary>
/// <param name="TournamentId">The tournament to read.</param>
public record TournamentActionGet(Guid TournamentId) : TournamentAction(TournamentId);

/// <summary>
/// Registers a player, while the tournament is <see cref="TournamentState.Registering"/> only; afterwards it fails
/// with <see cref="HoldemErrorCode.TournamentAlreadyStarted"/>. Once <see cref="TournamentOptionData.MaxEntries"/>
/// players are registered it fails with <see cref="HoldemErrorCode.TournamentFull"/>, and a name that is
/// <see langword="null"/>, empty, whitespace only, longer than 64 characters, or already registered (compared
/// ignoring case) fails with <see cref="HoldemErrorCode.InvalidEntry"/>. The new entry's
/// <see cref="TournamentEntryData.EntryNo"/> is one more than the highest ever given in this tournament, so a
/// number is never given twice, not even after a <see cref="TournamentActionUnregister"/>.
/// </summary>
/// <param name="TournamentId">The tournament to register for.</param>
/// <param name="Name">The player's display name.</param>
public record TournamentActionRegister(Guid TournamentId, string Name) : TournamentAction(TournamentId);

/// <summary>
/// Withdraws a registration, while the tournament is <see cref="TournamentState.Registering"/> only; afterwards it
/// fails with <see cref="HoldemErrorCode.TournamentAlreadyStarted"/>. An <paramref name="EntryNo"/> that is not
/// registered fails with <see cref="HoldemErrorCode.InvalidEntry"/>. The entry leaves
/// <see cref="TournamentData.Entries"/> and its buy-in leaves the <see cref="TournamentData.PrizePool"/>.
/// </summary>
/// <param name="TournamentId">The tournament to withdraw from.</param>
/// <param name="EntryNo">The <see cref="TournamentEntryData.EntryNo"/> to withdraw.</param>
public record TournamentActionUnregister(Guid TournamentId, int EntryNo) : TournamentAction(TournamentId);

/// <summary>
/// Starts a tournament that is <see cref="TournamentState.Registering"/>; afterwards it fails with
/// <see cref="HoldemErrorCode.TournamentAlreadyStarted"/>, and with fewer than
/// <see cref="TournamentOptionData.MinEntries"/> entries it fails with <see cref="HoldemErrorCode.NotEnoughPlayers"/>.
/// <para>
/// Registration closes and the seats are drawn at random: the entries are split into as few tables of
/// <see cref="TournamentOptionData.MaxPlayersPerTable"/> seats as they fit in, whose sizes differ by at most one, each
/// seated from seat 1 up with a button drawn among its seats. Each table is an ordinary game, listed in
/// <see cref="TournamentData.Tables"/>, whose first hand is dealt at once at the first level with every player at
/// <see cref="TournamentOptionData.StartingStack"/>; a table drawn with a single player waits without a hand. The
/// level clock starts now, and the prize pool is fixed.
/// </para>
/// <para>
/// Players act on their table through <see cref="IHoldemTable"/> with its game id, which takes <c>Play</c> and
/// <c>GetGame</c> only: every other action on a tournament table fails with
/// <see cref="HoldemErrorCode.ManagedByTournament"/>.
/// </para>
/// </summary>
/// <param name="TournamentId">The tournament to start.</param>
public record TournamentActionStart(Guid TournamentId) : TournamentAction(TournamentId)
{
    /// <summary>
    /// The seed of the seat draw; <see langword="null"/> (the default) draws a seed at random. Either way the seed
    /// is kept, so a start can be reproduced: one seed gives one draw of seats and buttons for the same entries.
    /// </summary>
    public int? SeatDrawSeed { get; init; }
}

/// <summary>
/// The host's call between two hands of one table of a running tournament: nothing deals a table's next hand by
/// itself. It fails with <see cref="HoldemErrorCode.TournamentNotStarted"/> while the tournament is registering, with
/// <see cref="HoldemErrorCode.TournamentFinished"/> once it has finished, with <see cref="HoldemErrorCode.InvalidTable"/>
/// for a table number the tournament does not have or a table that has been broken up, and with
/// <see cref="HoldemErrorCode.PreviousHandNotEnded"/> while the table's hand is still being played.
/// <para>
/// Every player at the table who ended its last hand without chips is eliminated. With <c>P</c> players left before
/// the call and <c>b</c> of them out now, they take places <c>P - b + 1</c> to <c>P</c>, the bigger stack at the start
/// of that hand taking the better place. Players who started that hand with equal stacks tie: each gets the best
/// place of the group, and they share the prizes of the places the group covers equally, to the cent, any cent left
/// over going to the lower <see cref="TournamentEntryData.EntryNo"/>. The prize of place <c>k</c> is
/// <see cref="TournamentData.PrizePool"/> times the <c>k</c>-th of <see cref="TournamentOptionData.Payouts"/>, divided
/// by 100 and rounded to the cent, and <c>0</c> past the last payout; first place gets what the other paid places
/// leave of the pool, so the prizes add up to it exactly. An eliminated player's seat is freed.
/// </para>
/// <para>
/// When one player is left, that player takes first place and its prize, the tournament becomes
/// <see cref="TournamentState.Finished"/>, and every table's game is finished, after which <c>Play</c> on it fails with
/// <see cref="HoldemErrorCode.GameFinished"/>. Otherwise, when the table still has at least two players, its next hand
/// is dealt at the level the tournament's clock shows now; a hand already in play keeps the level it was dealt at. A
/// table left with fewer than two players deals nothing and waits; the call still succeeds.
/// </para>
/// <para>
/// The call saves the tournament and that table's game together, so two tables never wait for each other; a lost race
/// with another call on the same tournament fails with <see cref="HoldemErrorCode.Conflict"/>.
/// </para>
/// </summary>
/// <param name="TournamentId">The tournament the table belongs to.</param>
/// <param name="TableNo">The <see cref="TournamentTableData.TableNo"/> of the table to deal.</param>
public record TournamentActionDealTable(Guid TournamentId, int TableNo) : TournamentAction(TournamentId);
