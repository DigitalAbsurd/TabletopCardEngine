using AntDesign;
using Microsoft.AspNetCore.Components;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;
using System.Globalization;

namespace OpenHoldemEngine.Demo.Components.Table;

/// <summary>
/// The action bar along the bottom of the poker table. Fold, Check, Call and Raise are shown only
/// when the matching action is available in HandData.Evaluations; the raise is sized with a slider,
/// −/+ steps of one big blind and a Quick Actions dropdown of half-pot, pot and all-in presets that
/// set the amount without submitting it. Once the hand is finished the bar offers Next Hand and New Game instead.
/// </summary>
public partial class ActionPanel : ComponentBase
{
    /// <summary>The current hand data with available actions.</summary>
    [Parameter]
    public HandData? HandData { get; set; }

    /// <summary>
    /// The size of one −/+ step on the raise amount. The hand carries no blinds, so the table
    /// passes the game's own.
    /// </summary>
    [Parameter]
    public decimal BigBlind { get; set; }

    /// <summary>Raised by Next Hand once the hand is finished.</summary>
    [Parameter]
    public EventCallback OnNextHand { get; set; }

    /// <summary>Raised by New Game once the hand is finished; the host opens the settings modal.</summary>
    [Parameter]
    public EventCallback OnNewGame { get; set; }

    /// <summary>The demo session to apply actions through.</summary>
    [Inject]
    public DemoGameSession Session { get; set; } = null!;

    /// <summary>The Quick Actions menu opens on a click, not on hover, so it never covers the bar uninvited.</summary>
    private static readonly Trigger[] _quickActionsTrigger = [Trigger.Click];

    private decimal _raiseAmount;
    private bool _isSubmitting;

    /// <summary>
    /// The hand the raise amount was last reset for. The table re-renders this component whenever
    /// the page does — switching a companion tab is enough — and a record parameter always counts as
    /// changed, so resetting on every parameter set would throw away an amount the visitor is still
    /// dialling in.
    /// </summary>
    private HandData? _raiseAmountResetFor;

    private bool IsHandFinished => HandData?.Street == Street.Finished;

    private bool HasActions => HandData is not null && HandData.NextPlayerSeatNo != 0 && HandData.Evaluations.Length > 0;

    /// <summary>
    /// The box on the right of the bar: whose decision is owed, or that the hand is over.
    /// </summary>
    private string InfoText
    {
        get
        {
            if (IsHandFinished)
            {
                return "Hand complete";
            }

            if (HandData is null || HandData.NextPlayerSeatNo == 0)
            {
                return "Waiting for the next hand";
            }

            var name = HandData.Seats.FirstOrDefault(s => s.SeatNo == HandData.NextPlayerSeatNo)?.Name;
            return $"{(string.IsNullOrWhiteSpace(name) ? $"Seat {HandData.NextPlayerSeatNo}" : name)} to act";
        }
    }

    /// <summary>
    /// Gets the street value for raise calculations.
    /// Raise MinValue/MaxValue are absolute totals (raise to this amount).
    /// </summary>
    private decimal TotalPot => (HandData?.Pot ?? 0) + (HandData?.StreetPot ?? 0);

    /// <summary>
    /// Gets the evaluation data for a specific action type, or null if not available.
    /// </summary>
    private PlayerActionEvaluationData? GetEvaluation(PlayerActionType actionType)
    {
        return HandData?.Evaluations.FirstOrDefault(e => e.PlayerActionType == actionType);
    }

    /// <summary>
    /// Checks if an action is available in the current evaluations.
    /// </summary>
    private bool HasAction(PlayerActionType actionType)
    {
        return GetEvaluation(actionType) is not null;
    }

    /// <summary>
    /// Gets the human-readable label for a Call action, showing the amount to call.
    /// Call's MinValue is an increment (how much more to put in).
    /// </summary>
    private string GetCallLabel()
    {
        var eval = GetEvaluation(PlayerActionType.Call);
        if (eval is null)
        {
            return "Call";
        }

        // MinValue for Call is the increment (how much more to put in)
        return eval.MinValue > 0 ? $"Call {eval.MinValue:F2}" : "Call";
    }

    /// <summary>
    /// Gets the min and max values for raise sizing.
    /// Raise's MinValue and MaxValue are absolute street totals (raise to this amount).
    /// </summary>
    private (decimal Min, decimal Max) GetRaiseWindow()
    {
        var eval = GetEvaluation(PlayerActionType.Raise);
        if (eval is null)
        {
            return (0, 0);
        }

        return (eval.MinValue, eval.MaxValue);
    }

    /// <summary>
    /// Checks if the raise window is degenerate (all-in only).
    /// This is indicated by MinValue == MaxValue.
    /// </summary>
    private bool IsAllInOnly()
    {
        var (min, max) = GetRaiseWindow();
        return min == max && min > 0;
    }

    /// <summary>
    /// Gets the all-in amount when the window is degenerate.
    /// </summary>
    private decimal GetAllInAmount()
    {
        var (min, _) = GetRaiseWindow();
        return min;
    }

    /// <summary>
    /// Calculates half-pot amount, clamped to the legal raise window.
    /// </summary>
    private decimal GetHalfPotRaise()
    {
        var (min, max) = GetRaiseWindow();
        if (min == 0 && max == 0)
        {
            return 0;
        }

        var halfPot = (TotalPot / 2) + (HandData?.Seats.FirstOrDefault(s => s.SeatNo == HandData.NextPlayerSeatNo)?.Stack ?? 0);
        return Math.Clamp(halfPot, min, max);
    }

    /// <summary>
    /// Calculates pot amount, clamped to the legal raise window.
    /// </summary>
    private decimal GetPotRaise()
    {
        var (min, max) = GetRaiseWindow();
        if (min == 0 && max == 0)
        {
            return 0;
        }

        var potAmount = TotalPot + (HandData?.Seats.FirstOrDefault(s => s.SeatNo == HandData.NextPlayerSeatNo)?.Stack ?? 0);
        return Math.Clamp(potAmount, min, max);
    }

    /// <summary>
    /// Gets the all-in amount for the current seat, which is its stack plus current street wager.
    /// </summary>
    private decimal GetAllInRaise()
    {
        var (_, max) = GetRaiseWindow();
        return max; // MaxValue for raise is the all-in amount
    }

    /// <summary>
    /// Resets the raise amount to the minimum when a new hand state arrives — a new hand, or the
    /// next decision within one, both of which the engine hands back as a new HandData instance.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ReferenceEquals(HandData, _raiseAmountResetFor))
        {
            return;
        }

        _raiseAmountResetFor = HandData;
        if (HandData is not null && HasAction(PlayerActionType.Raise))
        {
            var (min, _) = GetRaiseWindow();
            _raiseAmount = min;
        }
    }

    /// <summary>
    /// Applies a Fold action through the session.
    /// </summary>
    private async Task OnFoldAsync()
    {
        await SubmitAsync(PlayerActionType.Fold, 0);
    }

    /// <summary>
    /// Applies a Check action through the session.
    /// </summary>
    private async Task OnCheckAsync()
    {
        await SubmitAsync(PlayerActionType.Check, 0);
    }

    /// <summary>
    /// Applies a Call action through the session.
    /// Call's MinValue is the increment, but the size sent to ApplyPlayerActionAsync should be 0
    /// because the engine computes the call amount itself.
    /// </summary>
    private async Task OnCallAsync()
    {
        await SubmitAsync(PlayerActionType.Call, 0);
    }

    /// <summary>
    /// Applies a Raise action through the session with the specified amount.
    /// The amount is the absolute street total (raise to this amount).
    /// </summary>
    private async Task OnRaiseAsync(decimal amount)
    {
        await SubmitAsync(PlayerActionType.Raise, amount);
    }

    private async Task SubmitAsync(PlayerActionType actionType, decimal size)
    {
        _isSubmitting = true;
        try
        {
            await Session.ApplyPlayerActionAsync(actionType, size);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task OnNextHandAsync()
    {
        _isSubmitting = true;
        try
        {
            await OnNextHand.InvokeAsync();
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task OnNewGameAsync()
    {
        await OnNewGame.InvokeAsync();
    }

    /// <summary>
    /// Sets the raise amount from a preset, clamped to the legal window. Presets only size the
    /// raise; Raise is what submits it.
    /// </summary>
    private void SetRaiseAmount(decimal amount)
    {
        _raiseAmount = amount;
        ClampRaiseAmount();
    }

    /// <summary>
    /// Moves the raise amount by one big blind in either direction, clamped to the legal window.
    /// </summary>
    private void StepRaiseAmount(int direction)
    {
        var step = BigBlind > 0 ? BigBlind : 1m;
        SetRaiseAmount(_raiseAmount + (direction * step));
    }

    /// <summary>
    /// Clamps the raise amount to the legal window.
    /// </summary>
    private void ClampRaiseAmount()
    {
        var (min, max) = GetRaiseWindow();
        if (min > 0 && max > 0)
        {
            _raiseAmount = Math.Clamp(_raiseAmount, min, max);
        }
    }

    /// <summary>
    /// Handles changes to the raise amount input field.
    /// </summary>
    private void OnRaiseInputChanged(ChangeEventArgs e)
    {
        if (decimal.TryParse(e.Value?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            SetRaiseAmount(value);
        }
    }

    /// <summary>
    /// Handles the raise amount slider while it is being dragged.
    /// </summary>
    private void OnRaiseSliderChanged(ChangeEventArgs e)
    {
        if (decimal.TryParse(e.Value?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            _raiseAmount = value;
        }
    }

    /// <summary>
    /// Formats an amount for an input's value, min or max attribute, which the browser only parses
    /// with a dot for the decimal separator.
    /// </summary>
    private static string FormatInvariant(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
