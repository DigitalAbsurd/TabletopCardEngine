namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>Where one registered player is in a tournament.</summary>
public enum TournamentEntryState : byte
{
    /// <summary>Registered, and not seated yet.</summary>
    Registered = 1,

    /// <summary>Playing at a table: <see cref="TournamentEntryData.TableNo"/> and <see cref="TournamentEntryData.SeatNo"/> say where.</summary>
    Seated = 2,

    /// <summary>Taken off a table that broke or was balanced, and waiting to be seated at another one with the same stack.</summary>
    Moving = 3,

    /// <summary>Out of the tournament: <see cref="TournamentEntryData.Place"/> and <see cref="TournamentEntryData.Prize"/> are final.</summary>
    Eliminated = 4,

    /// <summary>The last player left, in first place.</summary>
    Winner = 5,
}
