using System.ComponentModel.DataAnnotations;

namespace OpenHoldemEngine.Data;

public class Game
{
    [Key] public Guid Id { get; set; }
    public decimal InitialSmallBlind { get; set; } = 1;
    public int AutoIncrementHands { get; set; } = 0;
    public int MaxPlayersCount { get; set; } = 6;
    public int MaxHandsCount { get; set; } = 0;
    public bool AllowRebuy { get; set; } = false;
}