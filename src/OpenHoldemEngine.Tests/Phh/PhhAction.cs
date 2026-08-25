using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Tests.Phh;

/// <summary>One entry of a PHH <c>actions</c> array. <see cref="Raw"/> is kept for failure messages.</summary>
internal abstract record PhhAction(string Raw)
{
    public override string ToString() => Raw;
}

/// <summary>An action a player chose, as opposed to a deal or a showdown reveal.</summary>
internal interface IPhhPlayerDecision
{
    /// <summary>Zero-based index into the hand's player arrays.</summary>
    int PlayerIndex { get; }
}

/// <summary><c>d dh p&lt;n&gt; &lt;cards&gt;</c> — deal hole cards.</summary>
internal sealed record PhhDealHoleAction(string Raw, int PlayerIndex, Card?[] Cards) : PhhAction(Raw);

/// <summary><c>d db &lt;cards&gt;</c> — deal board cards.</summary>
internal sealed record PhhDealBoardAction(string Raw, Card?[] Cards) : PhhAction(Raw);

/// <summary><c>p&lt;n&gt; f</c> — fold.</summary>
internal sealed record PhhFoldAction(string Raw, int PlayerIndex) : PhhAction(Raw), IPhhPlayerDecision;

/// <summary><c>p&lt;n&gt; cc</c> — check or call, whichever the position allows.</summary>
internal sealed record PhhCheckCallAction(string Raw, int PlayerIndex) : PhhAction(Raw), IPhhPlayerDecision;

/// <summary>
/// <c>p&lt;n&gt; cbr &lt;amount&gt;</c> — complete, bet or raise. <see cref="Amount"/> is the total
/// the player's wager on the current street is brought up *to*, not the increment.
/// </summary>
internal sealed record PhhCompleteBetRaiseAction(string Raw, int PlayerIndex, decimal Amount) : PhhAction(Raw), IPhhPlayerDecision;

/// <summary><c>p&lt;n&gt; sm [cards]</c> — show or muck at showdown.</summary>
internal sealed record PhhShowMuckAction(string Raw, int PlayerIndex, Card?[] Cards) : PhhAction(Raw);

/// <summary><c>p&lt;n&gt; sd [cards]</c> — stand pat or discard (draw variants only).</summary>
internal sealed record PhhStandPatDiscardAction(string Raw, int PlayerIndex, Card?[] Cards) : PhhAction(Raw);
