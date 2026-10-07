namespace TabletopCardEngine.Data;

internal class HandHoleCard
{
    public Guid GameId { get; set; }
    public int HandNo { get; set; }
    public Hand Hand { get; set; } = new();
    public Card Card { get; set; } = null!;
    public byte SeatNo { get; set; }
    public byte Sequence { get; set; }
}
