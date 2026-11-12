namespace TabletopCardEngine.Holdem.Data;

/// <summary>One blind level of a tournament as it is stored in <see cref="Tournament.Levels"/>: the big blind is twice the small blind.</summary>
internal sealed record TournamentLevel(decimal SmallBlind, decimal Ante, int DurationSeconds);
