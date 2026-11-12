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
