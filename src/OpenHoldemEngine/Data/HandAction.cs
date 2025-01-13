using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Data;

public class HandAction
{
    public Guid GameId { get; set; }
    public byte SeatNo { get; set; }
    public byte NextSeatNo { get; set; }
    public int HandNo { get; set; }
    public Hand Hand { get; set; } = new();
    public int No { get; set; }
    public Street Street { get; set; }
    public Street NextStreet { get; set; }
    public PlayActionType PlayAction { get; set; }
    public decimal Amount { get; set; }
    public decimal AddAmount { get; set; }
    public decimal Paid { get; set; }
}