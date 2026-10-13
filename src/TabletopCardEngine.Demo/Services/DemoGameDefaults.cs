using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// The settings a visitor gets without asking for any. The demo opens on a running game, so these
/// values are what it deals, and they are also what the New Game form starts from — one copy, so
/// "the defaults" cannot drift between the boot path and the form.
/// </summary>
public static class DemoGameDefaults
{
    /// <summary>Seats filled by default: six-handed, the size the engine's own corpora are recorded at.</summary>
    public const int MaxPlayers = 6;

    /// <summary>The small blind. The engine derives the big blind from it, so there is no second constant.</summary>
    public const decimal SmallBlind = 1m;

    /// <summary>Where the button starts. Seats are 1-based, and 0 means "no seat".</summary>
    public const byte DealerSeatNo = 1;

    /// <summary>The stack every seat sits down with.</summary>
    public const decimal BuyIn = 100m;

    /// <summary>The game options the demo boots with.</summary>
    public static GameOptionData CreateOptions()
        => new()
        {
            MaxPlayersCount = MaxPlayers,
            InitialSmallBlind = SmallBlind,
            InitialDealerSeatNo = DealerSeatNo,
        };

    /// <summary>
    /// The seats to fill, ascending by seat number — the engine walks the array in order to resolve
    /// the first hand's button, so the order is a requirement rather than a preference.
    /// </summary>
    /// <param name="maxPlayers">How many seats to fill.</param>
    /// <param name="buyIn">The stack each of them brings.</param>
    public static PlayerJoinData[] CreateJoinSeats(int maxPlayers = MaxPlayers, decimal buyIn = BuyIn)
        => Enumerable
            .Range(1, maxPlayers)
            .Select(seatNo => new PlayerJoinData($"Player {seatNo}", (byte)seatNo, buyIn))
            .ToArray();
}
