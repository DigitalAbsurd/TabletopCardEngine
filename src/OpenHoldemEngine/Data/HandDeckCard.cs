using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Data;

public class HandDeckCard : Card
{
    public Guid GameId { get; set; }
    public int HandNo { get; set; }
    public Hand Hand { get; set; } = new();
    public byte Sequence { get; set; }
}