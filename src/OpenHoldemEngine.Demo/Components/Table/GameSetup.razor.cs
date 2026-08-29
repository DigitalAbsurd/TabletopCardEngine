using Microsoft.AspNetCore.Components;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Components.Table;

/// <summary>
/// Game setup form component. Allows the user to configure the game options
/// (player count, blinds, dealer seat) and the participating players,
/// then creates and starts the game through the DemoGameSession.
/// </summary>
public partial class GameSetup : ComponentBase, IAsyncDisposable
{
    /// <summary>Injected demo game session service.</summary>
    [Inject]
    public required DemoGameSession Session { get; set; }

    private int _maxPlayers = 6;
    private decimal _smallBlind = 1;
    private byte _dealerSeat = 1;
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
        public decimal BuyInAmount { get; set; } = 100m;
    }

    protected override void OnInitialized()
    {
        InitializeDefaultPlayers();
        Session.EngineError += OnEngineError;
    }

    private void InitializeDefaultPlayers()
    {
        _players.Clear();
        for (byte i = 1; i <= _maxPlayers; i++)
        {
            _players.Add(new PlayerSetup { Name = $"Player {i}", SeatNo = i, BuyInAmount = 100m });
        }
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
                _players.Add(new PlayerSetup { Name = $"Player {i}", SeatNo = i, BuyInAmount = 100m });
            }
        }

        _maxPlayers = newMaxPlayers;
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
            _players.Add(new PlayerSetup { Name = $"Player {nextSeatNo}", SeatNo = nextSeatNo, BuyInAmount = 100m });
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

    private async Task CreateAndStartAsync()
    {
        if (!IsFormValid)
        {
            _errorMessage = "Please fill all fields with valid values";
            return;
        }

        _isLoading = true;
        _errorMessage = null;

        try
        {
            // Create game with options
            var gameOptions = new GameOptionData(
                MaxPlayersCount: _maxPlayers,
                InitialSmallBlind: _smallBlind,
                InitialDealerSeatNo: _dealerSeat);

            var createSuccess = await Session.CreateGameAsync(gameOptions);
            if (!createSuccess)
            {
                return; // Error will be shown by OnEngineError
            }

            // Prepare join seats in ascending order (required by engine)
            var joinSeats = _players
                .OrderBy(p => p.SeatNo)
                .Select(p => new PlayerJoinData(p.Name, p.SeatNo, p.BuyInAmount))
                .ToArray();

            var startSuccess = await Session.StartFirstHandAsync(joinSeats);
            if (!startSuccess)
            {
                return; // Error will be shown by OnEngineError
            }

            _errorMessage = null;
        }
        finally
        {
            _isLoading = false;
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        Session.EngineError -= OnEngineError;
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
