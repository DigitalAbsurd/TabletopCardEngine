using Microsoft.AspNetCore.Components;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Components.Table;

/// <summary>
/// Renders action controls for the current seat based on HandData.Evaluations.
/// Fold, Check, Call, and Raise buttons are shown only when the matching action
/// is available in the evaluations. Raise uses a slider with pot, half-pot, and all-in shortcuts.
/// </summary>
public partial class ActionPanel : ComponentBase
{
    /// <summary>The current hand data with available actions.</summary>
    [Parameter]
    public HandData? HandData { get; set; }

    /// <summary>The demo session to apply actions through.</summary>
    [Inject]
    public DemoGameSession Session { get; set; } = null!;

    private decimal _raiseAmount;
    private bool _isSubmitting;
    private bool _drawerVisible;

    /// <summary>
    /// Gets the Drawer title, naming the seat whose decision is owed.
    /// </summary>
    private string DrawerTitle => HandData is null ? string.Empty : $"Seat {HandData.NextPlayerSeatNo}'s action";

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
    /// Initializes the raise amount to the minimum when a new evaluation is shown.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Reset raise amount when hand changes
        if (HandData is not null && HasAction(PlayerActionType.Raise))
        {
            var (min, _) = GetRaiseWindow();
            _raiseAmount = min;
        }
    }

    /// <summary>
    /// Opens the Drawer holding the action controls for the seat on the clock.
    /// </summary>
    private void OnTriggerClick()
    {
        _drawerVisible = true;
    }

    /// <summary>
    /// Applies a Fold action through the session.
    /// </summary>
    private async Task OnFoldAsync()
    {
        _isSubmitting = true;
        try
        {
            await Session.ApplyPlayerActionAsync(PlayerActionType.Fold, 0);
        }
        finally
        {
            _isSubmitting = false;
            _drawerVisible = false;
        }
    }

    /// <summary>
    /// Applies a Check action through the session.
    /// </summary>
    private async Task OnCheckAsync()
    {
        _isSubmitting = true;
        try
        {
            await Session.ApplyPlayerActionAsync(PlayerActionType.Check, 0);
        }
        finally
        {
            _isSubmitting = false;
            _drawerVisible = false;
        }
    }

    /// <summary>
    /// Applies a Call action through the session.
    /// Call's MinValue is the increment, but the size sent to ApplyPlayerActionAsync should be 0
    /// because the engine computes the call amount itself.
    /// </summary>
    private async Task OnCallAsync()
    {
        _isSubmitting = true;
        try
        {
            await Session.ApplyPlayerActionAsync(PlayerActionType.Call, 0);
        }
        finally
        {
            _isSubmitting = false;
            _drawerVisible = false;
        }
    }

    /// <summary>
    /// Applies a Raise action through the session with the specified amount.
    /// The amount is the absolute street total (raise to this amount).
    /// </summary>
    private async Task OnRaiseAsync(decimal amount)
    {
        _isSubmitting = true;
        try
        {
            await Session.ApplyPlayerActionAsync(PlayerActionType.Raise, amount);
        }
        finally
        {
            _isSubmitting = false;
            _drawerVisible = false;
        }
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
        if (decimal.TryParse(e.Value?.ToString(), out var value))
        {
            _raiseAmount = value;
            ClampRaiseAmount();
        }
    }

    /// <summary>
    /// Handles changes to the raise amount slider.
    /// </summary>
    private void OnRaiseSliderChanged(ChangeEventArgs e)
    {
        if (decimal.TryParse(e.Value?.ToString(), out var value))
        {
            _raiseAmount = value;
        }
    }
}
