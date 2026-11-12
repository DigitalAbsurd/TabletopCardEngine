namespace TabletopCardEngine.Holdem.Data;

internal class TournamentEntry
{
    public Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
    public int EntryNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? TableNo { get; set; }
    public byte? SeatNo { get; set; }

    /// <summary>The stack a player takes along while moving between tables; <see langword="null"/> when not moving.</summary>
    public decimal? CarriedStack { get; set; }

    public int? Place { get; set; }
    public decimal Prize { get; set; }
    public int? EliminatedInHandNo { get; set; }
    public int? EliminatedAtTableNo { get; set; }
}
