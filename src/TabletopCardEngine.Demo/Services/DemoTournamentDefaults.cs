using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.Demo.Services;

/// <summary>A blind level preset of the tournament form: how long each of the ten levels lasts.</summary>
/// <param name="Label">What the form's list shows.</param>
/// <param name="LevelDuration">The length of every level.</param>
public sealed record DemoTournamentLevelPreset(string Label, TimeSpan LevelDuration);

/// <summary>A payout preset of the tournament form: the share of the prize pool each paid place wins.</summary>
/// <param name="Label">What the form's list shows.</param>
/// <param name="Payouts">The percentages, first place first, summing to 100.</param>
public sealed record DemoTournamentPayoutPreset(string Label, IReadOnlyList<decimal> Payouts);

/// <summary>
/// The settings the New tournament form starts from and the presets it offers, in one place so the form,
/// the tests and the documentation of the demo cannot drift apart.
/// </summary>
public static class DemoTournamentDefaults
{
    /// <summary>Players registered by default: two tables at the default table size.</summary>
    public const int PlayerCount = 12;

    /// <summary>The fewest players the form takes.</summary>
    public const int MinPlayerCount = 2;

    /// <summary>The most players the form takes.</summary>
    public const int MaxPlayerCount = 30;

    /// <summary>The chips every player starts with.</summary>
    public const decimal StartingStack = 1500m;

    /// <summary>What each player adds to the prize pool.</summary>
    public const decimal BuyIn = 10m;

    /// <summary>The small blinds of the ten levels, which every level preset shares: 10/20 up to 300/600.</summary>
    private static readonly decimal[] _smallBlinds = [10m, 15m, 20m, 30m, 50m, 75m, 100m, 150m, 200m, 300m];

    /// <summary>The antes of the ten levels: none for the first three, then one per level.</summary>
    private static readonly decimal[] _antes = [0m, 0m, 0m, 5m, 10m, 15m, 20m, 30m, 40m, 60m];

    /// <summary>The level presets, the default (<see cref="Regular"/>) first.</summary>
    public static IReadOnlyList<DemoTournamentLevelPreset> LevelPresets { get; } =
    [
        new("Regular: 15-minute levels", TimeSpan.FromMinutes(15)),
        new("Turbo: 5-minute levels", TimeSpan.FromMinutes(5)),
    ];

    /// <summary>The payout presets, from the fewest paid places to the most.</summary>
    public static IReadOnlyList<DemoTournamentPayoutPreset> PayoutPresets { get; } =
    [
        new("Winner takes all", [100m]),
        new("65 / 35", [65m, 35m]),
        new("50 / 30 / 20", [50m, 30m, 20m]),
        new("40 / 25 / 15 / 12 / 8", [40m, 25m, 15m, 12m, 8m]),
    ];

    /// <summary>The preset the form starts on.</summary>
    public static DemoTournamentLevelPreset Regular => LevelPresets[0];

    /// <summary>The payout presets a tournament of this many players can pay: none has more places than players.</summary>
    /// <param name="playerCount">The number of players.</param>
    public static IEnumerable<DemoTournamentPayoutPreset> PayoutPresetsFor(int playerCount)
        => PayoutPresets.Where(preset => preset.Payouts.Count <= playerCount);

    /// <summary>The ten levels of a preset: blinds 10/20 up to 300/600, antes from level 4.</summary>
    /// <param name="preset">The preset that decides how long a level lasts.</param>
    public static TournamentLevelData[] CreateLevels(DemoTournamentLevelPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        return _smallBlinds
            .Select((smallBlind, index) => new TournamentLevelData
            {
                SmallBlind = smallBlind,
                Ante = _antes[index],
                Duration = preset.LevelDuration,
            })
            .ToArray();
    }

    /// <summary>The names the form registers: "Player 1" and on.</summary>
    /// <param name="playerCount">The number of players.</param>
    public static string[] CreatePlayerNames(int playerCount)
        => Enumerable.Range(1, playerCount).Select(number => $"Player {number}").ToArray();

    /// <summary>
    /// The options of a tournament of the form's settings. The minimum entries are the player count, so a
    /// payout preset with that many places or fewer always fits.
    /// </summary>
    /// <param name="playerCount">How many players take part.</param>
    /// <param name="tableFormat">The format of every table.</param>
    /// <param name="playersPerTable">The seats of every table.</param>
    /// <param name="bettingStructure">How bets are sized.</param>
    /// <param name="raiseCap">The raise cap of Fixed-Limit, <c>null</c> for none.</param>
    /// <param name="startingStack">The chips every player starts with.</param>
    /// <param name="buyIn">What each player adds to the prize pool.</param>
    /// <param name="levelPreset">The blind levels.</param>
    /// <param name="anteType">Who posts the ante.</param>
    /// <param name="payouts">The payout preset.</param>
    public static TournamentOptionData CreateOptions(
        int playerCount,
        HoldemTableFormat tableFormat,
        int playersPerTable,
        BettingStructure bettingStructure,
        int? raiseCap,
        decimal startingStack,
        decimal buyIn,
        DemoTournamentLevelPreset levelPreset,
        AnteType anteType,
        DemoTournamentPayoutPreset payouts)
    {
        ArgumentNullException.ThrowIfNull(payouts);
        return new TournamentOptionData
        {
            BettingStructure = bettingStructure,
            TableFormat = tableFormat,
            MaxPlayersPerTable = playersPerTable,
            RaiseCap = raiseCap,
            AnteType = anteType,
            StartingStack = startingStack,
            BuyIn = buyIn,
            MinEntries = playerCount,
            MaxEntries = playerCount,
            Levels = CreateLevels(levelPreset),
            Payouts = payouts.Payouts,
        };
    }
}
