using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Demo.Phh;

/// <summary>
/// Determines which PHH hands the engine can model.
///
/// Extracted from <c>OpenHoldemEngine.Tests.Phh.PhhReplayRunner.IsSupported</c> for use in the demo.
/// The engine plays no-limit hold'em with no antes and a big blind of exactly twice the small blind
/// (derived by <c>StartHandActionHandler</c>), and it can only settle a showdown the history wrote
/// every card of.
///
/// Recorded finishing stacks are deliberately *not* required. A corpus that has them is verified
/// against them; one that does not is still worth replaying for the positions, the action order,
/// the street progression and the board, which is most of what a hand history pins down.
/// </summary>
public static class PhhSupport
{
    /// <summary>
    /// Whether the engine can model this hand at all.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <param name="reason">If false, a human-readable rejection reason from the engine's constraints.</param>
    /// <returns>true if the hand can be replayed, false otherwise.</returns>
    public static bool IsSupported(PhhHand hand, out string reason)
    {
        reason = string.Empty;

        if (hand.Variant != "NT")
        {
            reason = $"variant '{hand.Variant}' is not no-limit Texas hold'em";
        }
        else if (hand.PlayerCount < 2)
        {
            reason = $"{hand.PlayerCount} players — a hand needs at least two";
        }
        else if (hand.BlindsOrStraddles.Length != hand.PlayerCount || hand.Antes.Length != hand.PlayerCount)
        {
            reason = "the antes and blinds do not line up with the players";
        }
        else if (hand.Antes.Any(ante => ante != 0))
        {
            reason = "antes are not supported by the engine";
        }
        else if (hand.SmallBlind <= 0 || hand.BigBlind != hand.SmallBlind * 2)
        {
            reason = $"blinds {hand.SmallBlind}/{hand.BigBlind} — the engine derives the big blind as twice the small blind";
        }
        else if (hand.BlindsOrStraddles.Skip(2).Any(blind => blind != 0))
        {
            reason = "straddles are not supported by the engine";
        }
        else if (hand.StartingStacks.Any(stack => stack is null))
        {
            reason = "not every starting stack is recorded";
        }
        else if (!ShowdownIsFullyRecorded(hand))
        {
            reason = "a showdown is reached without every contender's cards and the whole board recorded";
        }

        return reason.Length == 0;
    }

    /// <summary>
    /// Whether a hand that ends with more than one player still in it records everything needed to
    /// settle the showdown the same way the history did. <see cref="PhhHandDeck"/> substitutes unused
    /// deck cards for whatever a history leaves out, which costs nothing on a hand decided by folds
    /// but would have the engine award the pot to somebody the finishing stacks disagree with.
    /// Corpora of bot play record every card; corpora of real play record only what was shown.
    /// </summary>
    private static bool ShowdownIsFullyRecorded(PhhHand hand)
    {
        const int boardCardsCount = 5;
        var folded = hand.Actions.OfType<PhhFoldAction>().Select(fold => fold.PlayerIndex).ToHashSet();
        var contenders = Enumerable.Range(0, hand.PlayerCount).Where(playerIx => !folded.Contains(playerIx)).ToList();

        if (contenders.Count < 2)
        {
            return true;
        }

        var board = hand.Board;
        return board.Length == boardCardsCount
            && board.All(card => card is not null)
            && contenders.All(playerIx => IsFullyKnown(PhhHandDeck.RecordedHoleCards(hand, playerIx)));

        static bool IsFullyKnown(Card?[] hole) => hole.Length == 2 && hole.All(card => card is not null);
    }
}
