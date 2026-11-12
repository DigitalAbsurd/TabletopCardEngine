using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabletopCardEngine.Holdem.Data;

internal class Tournament
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public BettingStructure BettingStructure { get; set; } = BettingStructure.NoLimit;
    public HoldemTableFormat TableFormat { get; set; } = HoldemTableFormat.SixMax;
    public int MaxPlayersPerTable { get; set; } = 6;

    /// <summary>The most bets a Fixed-Limit street may hold; <see langword="null"/> means no cap.</summary>
    public int? RaiseCap { get; set; }

    public AnteType AnteType { get; set; } = AnteType.Classic;
    public decimal StartingStack { get; set; }
    public decimal BuyIn { get; set; }
    public int MinEntries { get; set; }
    public int? MaxEntries { get; set; }

    /// <summary>The blind levels in order. Stored as JSON in one column of at most <see cref="TournamentJson.MaxLength"/> characters.</summary>
    public List<TournamentLevel> Levels { get; set; } = [];

    /// <summary>The prize of each finishing place, first place first. Stored as JSON in one column of at most <see cref="TournamentJson.MaxLength"/> characters.</summary>
    public List<decimal> Payouts { get; set; } = [];

    /// <summary>The tournament's state: <c>1</c> registering, <c>2</c> running, <c>3</c> finished.</summary>
    public byte State { get; set; } = 1;

    public int? SeatDrawSeed { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Optimistic concurrency token, configured like <see cref="Game.Version"/>.</summary>
    public int Version { get; set; }

    public List<TournamentEntry> Entries { get; set; } = [];
    public List<TournamentTable> Tables { get; set; } = [];
}
