using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenHoldemEngine.Data;

internal class Game
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    public decimal InitialSmallBlind { get; set; } = 1;
    public int MaxPlayersCount { get; set; } = 6;
    public byte InitialDealerSeatNo { get; set; } = 1;

    public List<Player> Players { get; set; } = [];
    public List<Hand> Hands { get; set; } = [];
    public List<BuyIn> BuyIns { get; set; } = [];
}
