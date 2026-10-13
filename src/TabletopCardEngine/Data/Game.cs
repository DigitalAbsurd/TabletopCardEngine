using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Data;

internal class Game
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    public decimal InitialSmallBlind { get; set; } = 1;
    public int MaxPlayersCount { get; set; } = 6;
    public byte InitialDealerSeatNo { get; set; } = 1;
    public GameType GameType { get; set; } = GameType.Holdem;
    public BettingStructure BettingStructure { get; set; } = BettingStructure.NoLimit;

    public bool IsFinished { get; set; }

    /// <summary>Optimistic concurrency token. Application-incremented on every saving call, since SQLite has no rowversion.</summary>
    public int Version { get; set; }

    public List<Player> Players { get; set; } = [];
    public List<Hand> Hands { get; set; } = [];
    public List<BuyIn> BuyIns { get; set; } = [];
}
