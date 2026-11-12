namespace TabletopCardEngine.Holdem.Data;

/// <summary>One table of a tournament: an ordinary <see cref="Game"/>, which does not know it belongs to one.</summary>
internal class TournamentTable
{
    public Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
    public int TableNo { get; set; }
    public Guid GameId { get; set; }
    public bool IsBroken { get; set; }
}
