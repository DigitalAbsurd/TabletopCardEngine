namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>Where a tournament is in its life. A tournament only moves forward through these states.</summary>
public enum TournamentState : byte
{
    /// <summary>Players can register and unregister; no table exists yet.</summary>
    Registering = 1,

    /// <summary>Registration is closed and the tables are playing.</summary>
    Running = 2,

    /// <summary>One player is left; the places and prizes are final.</summary>
    Finished = 3,
}
