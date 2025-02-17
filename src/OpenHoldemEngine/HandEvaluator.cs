using NPokerEngine.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine;
internal class HandEvaluator : global::NPokerEngine.Engine.IHandEvaluator
{
    private static HandEvaluator _instance = null!;

    private HandEvaluator()
    {
    }

    internal static HandEvaluator Instance => _instance ??= new HandEvaluator();

    private static Dictionary<HoldemHand.Hand.HandTypes, global::NPokerEngine.Types.HandRankType> _handTypeMapping =
        new Dictionary<HoldemHand.Hand.HandTypes, global::NPokerEngine.Types.HandRankType>
        {
                    { HoldemHand.Hand.HandTypes.Flush, global::NPokerEngine.Types.HandRankType.FLASH },
                    { HoldemHand.Hand.HandTypes.FullHouse, global :: NPokerEngine.Types.HandRankType.FULLHOUSE },
                    { HoldemHand.Hand.HandTypes.Pair, global :: NPokerEngine.Types.HandRankType.ONEPAIR },
                    { HoldemHand.Hand.HandTypes.Straight, global :: NPokerEngine.Types.HandRankType.STRAIGHT },
                    { HoldemHand.Hand.HandTypes.StraightFlush, global::NPokerEngine.Types.HandRankType.STRAIGHTFLASH },
                    { HoldemHand.Hand.HandTypes.FourOfAKind, global :: NPokerEngine.Types.HandRankType.FOURCARD },
                    { HoldemHand.Hand.HandTypes.TwoPair, global :: NPokerEngine.Types.HandRankType.TWOPAIR },
                    { HoldemHand.Hand.HandTypes.HighCard, global::NPokerEngine.Types.HandRankType.HIGHCARD },
                    { HoldemHand.Hand.HandTypes.Trips, global::NPokerEngine.Types.HandRankType.THREECARD },
        };

    public int EvalHand(IEnumerable<NCard> hole, IEnumerable<NCard> community)
    {
        return (int)GetPockerHand(hole, community).HandValue;
    }

    public HandRankInfo GenHandRankInfo(IEnumerable<NCard> hole, IEnumerable<NCard> community)
    {
        var hand = GetPockerHand(hole, community);
        return new global::NPokerEngine.Types.HandRankInfo
        {
            HandHigh = (int)hand.BoardMask,
            HandLow = 0,
            HoleHigh = (int)hand.PocketMask,
            HoleLow = 0,
            HandStrength = _handTypeMapping[hand.HandTypeValue],
        };
    }

    private static HoldemHand.Hand GetPockerHand(IEnumerable<global::NPokerEngine.Types.Card> hole, IEnumerable<global::NPokerEngine.Types.Card> community)
    {
        return new HoldemHand.Hand(pocket: ConvertCards(hole), board: ConvertCards(community));
    }

    private static string ConvertCards(IEnumerable<global::NPokerEngine.Types.Card> cards)
    {
        return string.Join(" ", cards.Select(card => card.ToString()).Select(card => $"{card[1].ToString().ToUpper()}{card[0].ToString().ToLower()}"));
    }
}
