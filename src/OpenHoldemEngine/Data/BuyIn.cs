namespace OpenHoldemEngine.Data;

internal class BuyIn
{
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    public byte SeatNo { get; set; }
    public int HandNo { get; set; }
    public decimal Amount { get; set; }
}