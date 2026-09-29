using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Components.Table;

/// <summary>
/// The action bar along the bottom of the poker table. Fold, Check, Call and Raise are shown only
/// when the matching action is available in HandData.Evaluations; the raise is sized with a slider
/// whose Min and All-in marks set the amount without submitting it, −/+ steps of one small blind, a
/// validated amount box with an auto-correct button, and ½ Pot / Pot buttons. Once the hand is
/// finished the bar offers Next Hand instead; New Game is in the page header.
/// </summary>
public partial class ActionPanel : ComponentBase
{
    /// <summary>The current hand data with available actions.</summary>
    [Parameter]
    public HandData? HandData { get; set; }

    /// <summary>
    /// One step of the raise amount: the small blind, the smallest chip on the table. −/+ move by
    /// it, and the slider, the presets and the auto-correct button round to it. The hand carries no
    /// blinds, so the table passes the game's own.
    /// </summary>
    [Parameter]
    public decimal RaiseStep { get; set; }

    /// <summary>Raised by Next Hand once the hand is finished.</summary>
    [Parameter]
    public EventCallback OnNextHand { get; set; }

    /// <summary>The demo session to apply actions through.</summary>
    [Inject]
    public DemoGameSession Session { get; set; } = null!;

    /// <summary>The last legal "raise to" amount; what Raise submits.</summary>
    private decimal _raiseAmount;

    /// <summary>What the amount box holds, which may not be a legal raise while the visitor types.</summary>
    private string _raiseText = string.Empty;

    /// <summary><see cref="_raiseText"/> read against the raise window.</summary>
    private RaiseInput _raiseInput = new(null, null);

    /// <summary>½ Pot and Pot for the current decision, from <see cref="RaiseSizing.Presets"/>.</summary>
    private IReadOnlyList<RaiseMark> _presets = [];

    /// <summary>The slider's reference amounts for the current decision, from <see cref="RaiseSizing.Marks"/>.</summary>
    private IReadOnlyList<RaiseMark> _marks = [];
    private bool _isSubmitting;

    /// <summary>
    /// The hand the raise amount was last reset for. The table re-renders this component whenever
    /// the page does — switching a tab is enough — and a record parameter always counts as
    /// changed, so resetting on every parameter set would throw away an amount the visitor is still
    /// dialling in.
    /// </summary>
    private HandData? _raiseAmountResetFor;

    private bool IsHandFinished => HandData?.Street == Street.Finished;

    private bool HasActions => HandData is not null && HandData.NextPlayerSeatNo != 0 && HandData.Evaluations.Count > 0;

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
        return eval.MinValue > 0 ? $"Call {AmountFormat.Format(eval.MinValue)}" : "Call";
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
    /// Resets the raise amount to the minimum when a new hand state arrives — a new hand, or the
    /// next decision within one, both of which the engine hands back as a new HandData instance.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        _marks = RaiseSizing.Marks(HandData);
        _presets = RaiseSizing.Presets(HandData, RaiseStep);
        if (ReferenceEquals(HandData, _raiseAmountResetFor))
        {
            return;
        }

        _raiseAmountResetFor = HandData;
        if (HandData is not null && HasAction(PlayerActionType.Raise))
        {
            var (min, _) = GetRaiseWindow();
            SetRaiseAmount(min);
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

    /// <summary>
    /// Sets the raise amount, clamped to the legal window, and writes it into the amount box. Marks,
    /// steps, presets and the amount box only size the raise; Raise is what submits it.
    /// </summary>
    private void SetRaiseAmount(decimal amount)
    {
        _raiseAmount = amount;
        ClampRaiseAmount();
        _raiseText = AmountFormat.Format(_raiseAmount);
        _raiseInput = new RaiseInput(_raiseAmount, null);
    }

    /// <summary>
    /// Moves the raise amount to the next multiple of the small blind in either direction, clamped to
    /// the legal window. A box holding something illegal steps from the last legal amount.
    /// </summary>
    private void StepRaiseAmount(int direction)
    {
        var (min, max) = GetRaiseWindow();
        SetRaiseAmount(RaiseSizing.Step(_raiseAmount, direction, RaiseStep, min, max));
    }

    /// <summary>What the auto-correct button would put in the box.</summary>
    private decimal CorrectedAmount
    {
        get
        {
            var (min, max) = GetRaiseWindow();
            return RaiseSizing.Correct(_raiseText, _raiseAmount, RaiseStep, min, max);
        }
    }

    /// <summary>The auto-correct button: puts the nearest legal amount in the box.</summary>
    private void CorrectRaiseAmount()
    {
        SetRaiseAmount(CorrectedAmount);
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
    /// Reads the amount box on every keystroke. A legal amount becomes the raise at once; anything
    /// else is left as typed, marked invalid and disables Raise until it is fixed — by typing, or by
    /// the auto-correct button — rather than being rewritten under the visitor's cursor.
    /// </summary>
    private void OnRaiseInput(ChangeEventArgs e)
    {
        _raiseText = e.Value?.ToString() ?? string.Empty;
        var (min, max) = GetRaiseWindow();
        _raiseInput = RaiseSizing.Validate(_raiseText, RaiseStep, min, max);
        if (_raiseInput.Amount is decimal amount)
        {
            _raiseAmount = amount;
        }
    }

    /// <summary>Enter in the amount box raises, when the amount is legal.</summary>
    private async Task OnRaisePressEnterAsync(KeyboardEventArgs e)
    {
        if (_raiseInput.IsValid && !_isSubmitting)
        {
            await OnRaiseAsync(_raiseAmount);
        }
    }

    /// <summary>
    /// Handles the raise amount slider while it is being dragged. The slider works in positions on
    /// the piecewise scale, not in chips.
    /// </summary>
    private void OnRaiseSliderChanged(double position)
    {
        SetRaiseAmount(RaiseSizing.FromPosition(_marks, position, RaiseStep));
    }

    /// <summary>The slider's tooltip shows the amount, not the position on its scale.</summary>
    private string FormatSliderTip(double position) => AmountFormat.Format(RaiseSizing.FromPosition(_marks, position, RaiseStep));
}
