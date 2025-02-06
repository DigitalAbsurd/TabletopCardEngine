namespace OpenHoldemEngine.Data;

internal class HandPlayerEndState
{
    public Guid GameId { get; set; }
    public int HandNo { get; set; }
    public Hand Hand { get; set; } = new();
    public byte SeatNo { get; set; }
    public decimal FinishStack { get; set; }
    public decimal WinAmount { get; set; }
}