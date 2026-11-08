using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.Data;

internal class Game
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    public decimal InitialSmallBlind { get; set; } = 1;
    public int MaxPlayersCount { get; set; } = 6;
    public byte InitialDealerSeatNo { get; set; } = 1;
    public GameType GameType { get; set; } = GameType.Holdem;
    public BettingStructure BettingStructure { get; set; } = BettingStructure.NoLimit;
    public HoldemTableFormat TableFormat { get; set; } = HoldemTableFormat.SixMax;
    public bool AllowTopUp { get; set; }
    public bool AllowRebuy { get; set; }
    public int? MaxRebuys { get; set; }
    public decimal? MinBuyIn { get; set; }
    public decimal? MaxBuyIn { get; set; }

    /// <summary>How the blinds rise; <see langword="null"/> means they never do. Stored as JSON in one column of at most <see cref="BlindScheduleJson.MaxLength"/> characters.</summary>
    public BlindScheduleData? BlindSchedule { get; set; }

    /// <summary>The most bets a Fixed-Limit street may hold; <see langword="null"/> means no cap, which is what every game created before 1.3 has.</summary>
    public int? RaiseCap { get; set; }

    public bool IsFinished { get; set; }

    /// <summary>Optimistic concurrency token. Application-incremented on every saving call, since SQLite has no rowversion.</summary>
    public int Version { get; set; }

    public List<Player> Players { get; set; } = [];
    public List<Hand> Hands { get; set; } = [];
    public List<BuyIn> BuyIns { get; set; } = [];
}
