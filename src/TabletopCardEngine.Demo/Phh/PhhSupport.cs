using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo.Phh;

/// <summary>
/// Determines which PHH hands the engine can model.
///
/// Extracted from <c>TabletopCardEngine.Tests.Phh.PhhReplayRunner.IsSupported</c> for use in the demo.
/// The engine plays no-limit, pot-limit and fixed-limit hold'em with a classic or a big blind ante (see <see cref="AnteFor"/>) and a big blind of exactly twice the small blind
/// (derived by <c>StartHandActionHandler</c>), and it can only settle a showdown the history wrote
/// every card of.
///
/// Recorded finishing stacks are deliberately *not* required. A corpus that has them is verified
/// against them; one that does not is still worth replaying for the positions, the action order,
/// the street progression and the board, which is most of what a hand history pins down.
/// </summary>
public static class PhhSupport
{
    /// <summary>The variant code of fixed-limit Texas hold'em.</summary>
    public const string FixedLimitVariant = "FT";

    /// <summary>The variant code of no-limit Texas hold'em.</summary>
    public const string NoLimitVariant = "NT";

    /// <summary>
    /// Not a code of the PHH specification: the test corpora name pot-limit Texas hold'em this way,
    /// by analogy with <c>NT</c> and <c>FT</c>.
    /// </summary>
    public const string PotLimitVariant = "PT";

    /// <summary>
    /// The betting structure a PHH variant code plays as, or <c>null</c> for a variant the engine does
    /// not play. The one place a variant becomes a structure.
    /// </summary>
    /// <param name="variant">The PHH variant code.</param>
    /// <returns>The structure, or null for an unsupported variant.</returns>
    public static BettingStructure? BettingStructureFor(string variant)
        => variant switch
        {
            NoLimitVariant => BettingStructure.NoLimit,
            FixedLimitVariant => BettingStructure.FixedLimit,
            PotLimitVariant => BettingStructure.PotLimit,
            _ => null,
        };

    /// <summary>The PHH variant code a betting structure is written as; the inverse of <see cref="BettingStructureFor"/>.</summary>
    /// <param name="structure">The betting structure of the game.</param>
    /// <returns>The variant code.</returns>
    public static string VariantFor(BettingStructure structure)
        => structure switch
        {
            BettingStructure.FixedLimit => FixedLimitVariant,
            BettingStructure.PotLimit => PotLimitVariant,
            _ => NoLimitVariant,
        };

    /// <summary>
    /// What each player posts as an ante, by seat. The file indexes <c>antes</c> like
    /// <c>blinds_or_straddles</c> and reverses the two entries heads-up, so there the big blind's ante,
    /// paid by <c>p1</c>, is the second entry.
    /// </summary>
    private static decimal[] AntesBySeat(PhhHand hand)
        => hand.PlayerCount == 2 ? [.. hand.Antes.Reverse()] : hand.Antes;

    /// <summary>
    /// The ante the engine plays a hand's <c>antes</c> as. Every player posting the same non-zero amount is
    /// a <see cref="AnteType.Classic"/> ante; the big blind's player alone posting one is a
    /// <see cref="AnteType.BigBlind"/> ante (heads-up included: the big blind is <c>p1</c> there); all zeros
    /// is none. Any other shape is not an ante the engine has, and <paramref name="reason"/> names it.
    /// </summary>
    /// <param name="hand">The hand whose antes are read.</param>
    /// <param name="ante">The nominal ante, or null when nobody posts one.</param>
    /// <param name="anteType">The type of the ante.</param>
    /// <param name="reason">If false, why the shape is not supported.</param>
    /// <returns>true if the engine has this ante, false otherwise.</returns>
    public static bool AnteFor(PhhHand hand, out decimal? ante, out AnteType anteType, out string reason)
    {
        ArgumentNullException.ThrowIfNull(hand);

        ante = null;
        anteType = AnteType.Classic;
        reason = string.Empty;

        var antes = AntesBySeat(hand);
        var posted = antes.Where(value => value != 0).ToArray();
        if (posted.Length == 0)
        {
            return true;
        }

        if (posted.Length == hand.PlayerCount && posted.Distinct().Count() == 1)
        {
            ante = posted[0];
            return true;
        }

        var bigBlindIx = hand.PlayerCount == 2 ? 0 : 1;
        if (posted.Length == 1 && antes[bigBlindIx] != 0)
        {
            ante = posted[0];
            anteType = AnteType.BigBlind;
            return true;
        }

        reason = posted.Length == 1
            ? "antes on seats other than the big blind"
            : posted.Length == hand.PlayerCount
                ? "antes of different sizes"
                : "antes on some seats but not all of them and not only the big blind";
        return false;
    }

    /// <summary>
    /// Whether the engine can model this hand at all.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <param name="reason">If false, a human-readable rejection reason from the engine's constraints.</param>
    /// <returns>true if the hand can be replayed, false otherwise.</returns>
    public static bool IsSupported(PhhHand hand, out string reason)
    {
        reason = string.Empty;

        if (BettingStructureFor(hand.Variant) is null)
        {
            reason = $"variant '{hand.Variant}' is not a Texas hold'em variant the engine plays";
        }
        else if (hand.Variant == FixedLimitVariant && (hand.SmallBet != hand.BigBlind || hand.BigBet != hand.BigBlind * 2))
        {
            reason = $"bets {hand.SmallBet}/{hand.BigBet} — the engine's fixed-limit bets are the big blind and twice it";
        }
        else if (hand.Variant == PotLimitVariant && hand.MinBet != hand.BigBlind)
        {
            reason = $"min bet {hand.MinBet} — the engine's smallest bet is the big blind";
        }
        else if (hand.Divergence is not null)
        {
            reason = $"pokerkit and the engine follow different rules here: {hand.Divergence}";
        }
        else if (hand.PlayerCount < 2)
        {
            reason = $"{hand.PlayerCount} players — a hand needs at least two";
        }
        else if (hand.PlayerCount > 10)
        {
            reason = "more than 10 players — the engine's largest table is Full Ring, 10 seats";
        }
        else if (hand.BlindsOrStraddles.Length != hand.PlayerCount || hand.Antes.Length != hand.PlayerCount)
        {
            reason = "the antes and blinds do not line up with the players";
        }
        else if (!AnteFor(hand, out _, out _, out var anteReason))
        {
            reason = anteReason;
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
