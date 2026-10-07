namespace TabletopCardEngine.Data;

internal class HandDeckCard
{
    public Guid GameId { get; set; }
    public int HandNo { get; set; }
    public Hand Hand { get; set; } = new();
    public Card Card { get; set; } = null!;
    public byte Sequence { get; set; }
}
