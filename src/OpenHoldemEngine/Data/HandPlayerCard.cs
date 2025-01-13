using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Data;

public class HandPlayerCard : Card
{
    public Guid GameId { get; set; }
    public int HandNo { get; set; }
    public Hand Hand { get; set; } = new();
    public byte SeatNo { get; set; }
    public byte Sequence { get; set; }
}