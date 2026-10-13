namespace TabletopCardEngine.Holdem.Data;

internal class HandPlayerStartState
{
    public Guid GameId { get; set; }
    public int HandNo { get; set; }
    public Hand Hand { get; set; } = new();
    public byte SeatNo { get; set; }
    public decimal StartStack { get; set; }
}
