using Microsoft.AspNetCore.Components;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Demo.Components.Table;

/// <summary>
/// The settings form for a new game: the game options (player count, blinds, dealer seat) and the
/// participating players. It is never a screen of its own — the host renders it inside the New Game
/// modal and drives it through <see cref="ApplyAsync"/>, which is why there is no submit button of
/// its own in the markup.
/// </summary>
public partial class GameSetup : ComponentBase, IAsyncDisposable
{
    /// <summary>Injected demo game session service.</summary>
    [Inject]
    public required DemoGameSession Session { get; set; }

    /// <summary>The replay controller, whose card shuffler has to be uninstalled before a normal game.</summary>
    [Inject]
    public required PhhReplayController ReplayController { get; set; }

    /// <summary>The engine's rows, which a new game starts without.</summary>
    [Inject]
    public required DemoDatabase Database { get; set; }

    private HoldemTableFormat _tableFormat = DemoGameDefaults.TableFormat;
    private int _maxPlayers = DemoGameDefaults.MaxPlayers;
    private decimal _smallBlind = DemoGameDefaults.SmallBlind;
    private byte _dealerSeat = DemoGameDefaults.DealerSeatNo;
    private readonly List<PlayerSetup> _players = [];
    private string? _errorMessage;
    private bool _isLoading;

    /// <summary>Represents a player being set up for a game.</summary>
    private sealed class PlayerSetup
    {
        /// <summary>Gets or sets the player's display name.</summary>
        public string Name { get; set; } = "Player";

        /// <summary>Gets or sets the seat number the player will occupy (1-based).</summary>
        public byte SeatNo { get; set; } = 1;

        /// <summary>Gets or sets the buy-in amount.</summary>
        public decimal BuyInAmount { get; set; } = DemoGameDefaults.BuyIn;
    }

    protected override void OnInitialized()
    {
        InitializeDefaultPlayers();
        Session.EngineError += OnEngineError;
    }

    /// <summary>
    /// Puts the form back to the demo's defaults and clears any error left by an earlier attempt,
    /// so reopening the modal never shows the previous visitor's half-filled state.
    /// </summary>
    public void ResetToDefaults()
    {
        _tableFormat = DemoGameDefaults.TableFormat;
        _maxPlayers = DemoGameDefaults.MaxPlayers;
        _smallBlind = DemoGameDefaults.SmallBlind;
        _dealerSeat = DemoGameDefaults.DealerSeatNo;
        _errorMessage = null;
        _isLoading = false;
        InitializeDefaultPlayers();
        StateHasChanged();
    }

    private void InitializeDefaultPlayers()
    {
        _players.Clear();
        for (byte i = 1; i <= _maxPlayers; i++)
        {
            _players.Add(new PlayerSetup { Name = $"Player {i}", SeatNo = i, BuyInAmount = DemoGameDefaults.BuyIn });
        }
    }

    /// <summary>
    /// Called when the table format changes. Sets Max Players to that format's default
    /// and adjusts the player list accordingly.
    /// </summary>
    private Task OnTableFormatChangedAsync(HoldemTableFormat newFormat)
    {
        _tableFormat = newFormat;

        var newMaxPlayers = newFormat switch
        {
            HoldemTableFormat.HeadsUp => 2,
            HoldemTableFormat.SixMax => 6,
            HoldemTableFormat.FullRing => 9,
            _ => 6,
        };

        return OnMaxPlayersChangedAsync(newMaxPlayers);
    }

    /// <summary>
    /// Called when the Max Players value changes. Handles trimming the player list if the
    /// value is lowered below the current player count, or adds empty slots if raised.
    /// </summary>
    private Task OnMaxPlayersChangedAsync(int newMaxPlayers)
    {
        if (newMaxPlayers < _maxPlayers)
        {
            // Lowering max players: trim the list to fit and renumber seats
            while (_players.Count > newMaxPlayers)
            {
                _players.RemoveAt(_players.Count - 1);
            }

            // Renumber the remaining players' seats to be sequential 1..newMaxPlayers
            for (int i = 0; i < _players.Count; i++)
            {
                _players[i].SeatNo = (byte)(i + 1);
                _players[i].Name = $"Player {i + 1}";
            }
        }
        else if (newMaxPlayers > _maxPlayers)
        {
            // Raising max players: add new players to fill the gap
            for (byte i = (byte)(_players.Count + 1); i <= newMaxPlayers; i++)
            {
                _players.Add(new PlayerSetup { Name = $"Player {i}", SeatNo = i, BuyInAmount = DemoGameDefaults.BuyIn });
            }
        }

        _maxPlayers = newMaxPlayers;

        // The dealer seat is bounded by the table size, and shrinking the table can leave it outside.
        if (_dealerSeat > _maxPlayers)
        {
            _dealerSeat = (byte)_maxPlayers;
        }

        return Task.CompletedTask;
    }

    private void OnEngineError(object? sender, EngineErrorEventArgs e)
    {
        _errorMessage = e.Message;
        _isLoading = false;
        StateHasChanged();
    }

    private void ClearErrorMessage()
    {
        _errorMessage = null;
    }

    private void AddPlayer()
    {
        if (_players.Count < _maxPlayers)
        {
            var nextSeatNo = (byte)(_players.Count + 1);
            _players.Add(new PlayerSetup
            {
                Name = $"Player {nextSeatNo}",
                SeatNo = nextSeatNo,
                BuyInAmount = DemoGameDefaults.BuyIn,
            });
        }
    }

    private void RemovePlayer(int index)
    {
        if (index >= 0 && index < _players.Count)
        {
            _players.RemoveAt(index);
        }
    }

    private bool IsFormValid
    {
        get
        {
            // At least 2 players
            if (_players.Count < 2)
            {
                return false;
            }

            // All players must have valid data
            foreach (var player in _players)
            {
                if (string.IsNullOrWhiteSpace(player.Name) || player.BuyInAmount <= 0)
                {
                    return false;
                }
            }

            // Dealer seat must be valid
            if (_dealerSeat < 1 || _dealerSeat > _maxPlayers)
            {
                return false;
            }

            // Small blind must be positive
            if (_smallBlind <= 0)
            {
                return false;
            }

            // Seats must be unique and in range
            var seatNumbers = _players.Select(p => p.SeatNo).ToList();
            if (seatNumbers.Count != seatNumbers.Distinct().Count())
            {
                return false;
            }

            if (seatNumbers.Any(s => s < 1 || s > _maxPlayers))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Wipes everything the previous game left behind and starts a new one under the settings
    /// currently in the form.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the engine calls.</param>
    /// <returns>
    /// <c>true</c> once the new game's first hand has been dealt; <c>false</c> when the form is
    /// invalid or an engine call failed, in which case the form shows the reason itself.
    /// </returns>
    public async Task<bool> ApplyAsync(CancellationToken cancellationToken = default)
    {
        if (!IsFormValid)
        {
            _errorMessage = "Please fill all fields with valid values";
            return false;
        }

        _isLoading = true;
        _errorMessage = null;

        try
        {
            // Order matters. Unload() is what hands the session back to the play engine: a normal
            // game dealt through the replay engine would deal the replayed hand's cards again.
            ReplayController.Unload();
            Session.Reset();
            Database.ClearAllData();

            var gameOptions = new GameOptionData { TableFormat = _tableFormat, MaxPlayersCount = _maxPlayers, InitialSmallBlind = _smallBlind, InitialDealerSeatNo = _dealerSeat };

            var createSuccess = await Session.CreateGameAsync(gameOptions, cancellationToken: cancellationToken);
            if (!createSuccess)
            {
                return false; // The reason is on screen already, raised through Session.EngineError.
            }

            // Ascending seat order is a requirement: the engine walks the array to resolve the button.
            var joinSeats = _players
                .OrderBy(p => p.SeatNo)
                .Select(p => new PlayerJoinData(p.Name, p.SeatNo, p.BuyInAmount))
                .ToArray();

            var startSuccess = await Session.StartFirstHandAsync(joinSeats, cancellationToken);
            if (!startSuccess)
            {
                return false;
            }

            _errorMessage = null;
        }
        finally
        {
            _isLoading = false;
        }

        return true;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        Session.EngineError -= OnEngineError;
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
