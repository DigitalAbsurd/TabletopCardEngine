using Microsoft.AspNetCore.Components;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.Demo.Components.Table;

/// <summary>
/// The settings form for a new tournament. Like <see cref="GameSetup"/> it is never a screen of its own: the
/// host renders it inside a modal and drives it through <see cref="ApplyAsync"/>, which registers the players
/// and starts the tournament in one go.
/// </summary>
public partial class TournamentSetup : ComponentBase, IAsyncDisposable
{
    /// <summary>Injected tournament session.</summary>
    [Inject]
    public required DemoTournamentSession TournamentSession { get; set; }

    /// <summary>Injected game session, which a tournament table is shown through.</summary>
    [Inject]
    public required DemoGameSession Session { get; set; }

    /// <summary>The replay controller, whose card shuffler has to be uninstalled before a tournament.</summary>
    [Inject]
    public required PhhReplayController ReplayController { get; set; }

    /// <summary>The engine's rows, which a new tournament starts without.</summary>
    [Inject]
    public required DemoDatabase Database { get; set; }

    private int _playerCount = DemoTournamentDefaults.PlayerCount;
    private HoldemTableFormat _tableFormat = DemoGameDefaults.TableFormat;
    private int _playersPerTable = DemoGameDefaults.MaxPlayers;
    private BettingStructure _bettingStructure = DemoGameDefaults.BettingStructure;
    private int? _raiseCap = DemoGameDefaults.RaiseCap;
    private bool _noRaiseCap;
    private decimal _startingStack = DemoTournamentDefaults.StartingStack;
    private decimal _buyIn = DemoTournamentDefaults.BuyIn;
    private int _levelPresetIndex;
    private AnteType _anteType = AnteType.Classic;
    private int _payoutIndex;
    private string? _errorMessage;
    private bool _isLoading;

    /// <summary>A payout preset with its place in <see cref="DemoTournamentDefaults.PayoutPresets"/>.</summary>
    private sealed record OfferedPayout(int Index, DemoTournamentPayoutPreset Preset);

    /// <summary>The payout presets this many players can pay, which is only those with no more places than players.</summary>
    private IEnumerable<OfferedPayout> OfferedPayouts
        => DemoTournamentDefaults.PayoutPresets
            .Select((preset, index) => new OfferedPayout(index, preset))
            .Where(offered => offered.Preset.Payouts.Count <= _playerCount);

    /// <summary>The chosen payout, or the largest one still on offer when fewer players no longer pay the chosen one.</summary>
    private int PayoutIndex
        => OfferedPayouts.Any(offered => offered.Index == _payoutIndex)
            ? _payoutIndex
            : OfferedPayouts.DefaultIfEmpty(new OfferedPayout(0, DemoTournamentDefaults.PayoutPresets[0])).Last().Index;

    /// <summary>
    /// Puts the form back to the demo's defaults and clears any error left by an earlier attempt, so
    /// reopening the modal never shows the previous visitor's half-filled state.
    /// </summary>
    public void ResetToDefaults()
    {
        _playerCount = DemoTournamentDefaults.PlayerCount;
        _tableFormat = DemoGameDefaults.TableFormat;
        _playersPerTable = DemoGameDefaults.MaxPlayers;
        _bettingStructure = DemoGameDefaults.BettingStructure;
        _raiseCap = DemoGameDefaults.RaiseCap;
        _noRaiseCap = false;
        _startingStack = DemoTournamentDefaults.StartingStack;
        _buyIn = DemoTournamentDefaults.BuyIn;
        _levelPresetIndex = 0;
        _anteType = AnteType.Classic;
        _payoutIndex = 0;
        _errorMessage = null;
        _isLoading = false;
        StateHasChanged();
    }

    /// <summary>The tournament options the form currently describes. The engine answers for their limits; nothing is clamped here.</summary>
    public TournamentOptionData BuildOptions()
    {
        var levelPreset = DemoTournamentDefaults.LevelPresets[Math.Clamp(_levelPresetIndex, 0, DemoTournamentDefaults.LevelPresets.Count - 1)];
        return DemoTournamentDefaults.CreateOptions(
            _playerCount,
            _tableFormat,
            _playersPerTable,
            _bettingStructure,
            _noRaiseCap ? null : _raiseCap,
            _startingStack,
            _buyIn,
            levelPreset,
            _anteType,
            DemoTournamentDefaults.PayoutPresets[PayoutIndex]);
    }

    /// <summary>
    /// Wipes everything the previous game left behind, then creates the tournament, registers the players and
    /// starts it under the settings currently in the form.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the engine calls.</param>
    /// <returns>
    /// <c>true</c> once the first hands have been dealt; <c>false</c> when the form is invalid or an engine
    /// call failed, in which case the form shows the reason itself.
    /// </returns>
    public async Task<bool> ApplyAsync(CancellationToken cancellationToken = default)
    {
        if (_playerCount is < DemoTournamentDefaults.MinPlayerCount or > DemoTournamentDefaults.MaxPlayerCount)
        {
            _errorMessage = $"A tournament takes {DemoTournamentDefaults.MinPlayerCount} to {DemoTournamentDefaults.MaxPlayerCount} players.";
            await InvokeAsync(StateHasChanged);
            return false;
        }

        _isLoading = true;
        _errorMessage = null;
        var failure = (string?)null;
        void Capture(object? sender, EngineErrorEventArgs e) => failure = e.Message;

        try
        {
            // Order matters, as for a new game: Unload() hands the session back to the play engine.
            ReplayController.Unload();
            Session.Reset();
            TournamentSession.Reset();
            Database.ClearAllData();

            TournamentSession.EngineError += Capture;
            var started = await TournamentSession.CreateAndStartAsync(
                BuildOptions(),
                DemoTournamentDefaults.CreatePlayerNames(_playerCount),
                cancellationToken: cancellationToken);
            if (!started)
            {
                _errorMessage = failure ?? "The tournament could not be started.";
                return false;
            }
        }
        finally
        {
            TournamentSession.EngineError -= Capture;
            _isLoading = false;
        }

        return true;
    }

    private Task OnTableFormatChanged(HoldemTableFormat format)
    {
        _tableFormat = format;
        _playersPerTable = format switch
        {
            HoldemTableFormat.HeadsUp => 2,
            HoldemTableFormat.FullRing => 9,
            _ => 6,
        };
        return Task.CompletedTask;
    }

    private Task OnPlayersPerTableChanged(int seats)
    {
        // A format change swaps the options, and while the chosen value is missing from the new list the
        // AntDesign Select reports default(int); that is not a pick (issue #544 for GameSetup).
        if (seats >= MinSeatsOf(_tableFormat) && seats <= MaxSeatsOf(_tableFormat))
        {
            _playersPerTable = seats;
        }

        return Task.CompletedTask;
    }

    private Task OnPayoutChanged(int index)
    {
        if (OfferedPayouts.Any(offered => offered.Index == index))
        {
            _payoutIndex = index;
        }

        return Task.CompletedTask;
    }

    private static int MinSeatsOf(HoldemTableFormat format) => format == HoldemTableFormat.FullRing ? 7 : 2;

    private static int MaxSeatsOf(HoldemTableFormat format) => format switch
    {
        HoldemTableFormat.HeadsUp => 2,
        HoldemTableFormat.FullRing => 10,
        _ => 6,
    };

    private void ClearErrorMessage()
    {
        _errorMessage = null;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
