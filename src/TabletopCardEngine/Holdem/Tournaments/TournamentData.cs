using TabletopCardEngine.Holdem.Mappers;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>Everything the engine knows about a tournament, as returned by every call.</summary>
/// <param name="TournamentId">Identifier of the tournament.</param>
/// <param name="Options">The rules the tournament was created with.</param>
/// <param name="State">Where the tournament is in its life.</param>
/// <param name="Version">
/// How many times the tournament has been saved, starting at <c>1</c> on creation. Pass it back as
/// <see cref="TournamentAction.ExpectedVersion"/> to have a stale write rejected with <see cref="HoldemErrorCode.Conflict"/>.
/// Reading the tournament does not change it.
/// </param>
public record TournamentData(Guid TournamentId, TournamentOptionData Options, TournamentState State, int Version)
{
    /// <summary>Every registered player, ascending by <see cref="TournamentEntryData.EntryNo"/>. Record equality compares it element by element for values the engine returns.</summary>
    public IReadOnlyList<TournamentEntryData> Entries { get; init; } = EquatableList<TournamentEntryData>.Empty;

    /// <summary>The tournament's tables, ascending by <see cref="TournamentTableData.TableNo"/>; empty until the tournament starts. Record equality compares it element by element for values the engine returns.</summary>
    public IReadOnlyList<TournamentTableData> Tables { get; init; } = EquatableList<TournamentTableData>.Empty;

    /// <summary>The current blind level, as an index into <see cref="TournamentOptionData.Levels"/>: <c>0</c> is the first level, as for <c>HandData.BlindLevel</c>.</summary>
    public int LevelIndex { get; init; }

    /// <summary>When the current level ends; <see langword="null"/> before the start and on the last level, which lasts until the tournament ends.</summary>
    public DateTimeOffset? LevelEndsAt { get; init; }

    /// <summary>When the tournament started; <see langword="null"/> while it is registering.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>When the tournament finished; <see langword="null"/> until it has.</summary>
    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>What the places share: <see cref="TournamentOptionData.BuyIn"/> times the number of entries.</summary>
    public decimal PrizePool { get; init; }
}

/// <summary>One registered player of a tournament.</summary>
/// <param name="EntryNo">The entry's number, given at registration: one more than the highest given before, never reused.</param>
/// <param name="Name">The player's display name, unique within the tournament regardless of case.</param>
public record TournamentEntryData(int EntryNo, string Name)
{
    /// <summary>Where the player is in the tournament.</summary>
    public TournamentEntryState State { get; init; } = TournamentEntryState.Registered;

    /// <summary>The table the player sits at; <see langword="null"/> when not seated.</summary>
    public int? TableNo { get; init; }

    /// <summary>The seat the player sits in at <see cref="TableNo"/>; <see langword="null"/> when not seated. Seats are 1-based.</summary>
    public byte? SeatNo { get; init; }

    /// <summary>The player's finishing place, <c>1</c> for the winner; <see langword="null"/> while still playing.</summary>
    public int? Place { get; init; }

    /// <summary>What the player won; <c>0</c> until a paid place is reached.</summary>
    public decimal Prize { get; init; }
}

/// <summary>One table of a tournament: an ordinary game, played through <see cref="IHoldemTable"/> with <paramref name="GameId"/>.</summary>
/// <param name="TableNo">The table's number within the tournament, ascending from 1.</param>
/// <param name="GameId">The game the table is played as.</param>
public record TournamentTableData(int TableNo, Guid GameId)
{
    /// <summary>Whether the table has been broken up and its players moved to other tables; a broken table deals no more hands.</summary>
    public bool IsBroken { get; init; }

    /// <summary>How many players are seated at the table.</summary>
    public int PlayerCount { get; init; }
}
