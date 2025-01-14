using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Data;
internal static class EntityExtensions
{
    internal static Hand GetCurrentHand(this Game game)
        => game.Hands.LastOrDefault() ?? new Hand();

    internal static HandAction GetLastAction(this Hand hand)
        => hand.Actions.LastOrDefault() ?? new HandAction();

    internal static List<Card> GetDeckCards(this Hand hand)
        => hand.DeckCards
            .OrderBy(c => c.Sequence)
            .Select(c => c.Card)
            .ToList();

    internal static Street GetStreet(this Hand hand)
        => hand.GetLastAction().NextStreet;

    internal static bool IsStarted(this Hand hand)
        => hand.Actions.Any();

    internal static bool TryGetBlindSeats(this Hand hand, out byte sbSeatNo, out byte bbSeatNo)
    {
        if (!hand.IsStarted())
        {
            sbSeatNo = 0;
            bbSeatNo = 0;
            return false;
        }

        sbSeatNo = hand.Actions.SingleOrDefault(act => act.PlayAction == PlayerActionType.SmallBlind)?.SeatNo
            ?? throw new ArgumentException(nameof(bbSeatNo));
        bbSeatNo = hand.Actions.SingleOrDefault(act => act.PlayAction == PlayerActionType.BigBlind)?.SeatNo
            ?? throw new ArgumentException(nameof(bbSeatNo));

        return true;
    }

    internal static Card[]? GetSeatHoleCards(this Hand hand, byte seatNo)
        => hand.HoleCards.Any(hc => hc.SeatNo == seatNo) ? hand.HoleCards.Where(hc => hc.SeatNo == seatNo).Select(hc => hc.Card).ToArray() : null;

    internal static decimal GetSeatStartStack(this Hand hand, byte seatNo)
        => hand.PlayerStartStates.Single(pss => pss.SeatNo == seatNo).StartStack;

    internal static decimal GetSeatCurrentStack(this Hand hand, byte seatNo)
        => hand.GetSeatStartStack(seatNo) - hand.GetSeatActions(seatNo).Select(group => group.Value.LastOrDefault()?.Amount ?? 0).Sum();

    internal static decimal GetSeatWinAmount(this Hand hand, byte seatNo)
        => hand.PlayerEndStates.SingleOrDefault(pss => pss.SeatNo == seatNo)?.WinAmount ?? 0;

    internal static decimal GetSeatBetAmount(this Hand hand, byte seatNo)
        => hand.GetSeatActions(seatNo)
            .Select(g => g.Value.Max(t => t.Amount))
            .Sum();

    internal static Dictionary<Street, List<HandAction>> GetSeatActions(this Hand hand, byte seatNo)
        => hand.Actions
            .Where(act => act.SeatNo == seatNo)
            .OrderBy(act => act.No)
            .GroupBy(act => act.Street)
            .ToDictionary(g => g.Key, g => g.ToList());

    internal static HandAction? GetSeatLastAction(this Hand hand, byte seatNo)
        => hand.Actions
            .Where(act => act.SeatNo == seatNo)
            .LastOrDefault();

    internal static decimal GetSeatActualStack(this Hand hand, byte seatNo)
        => hand.GetSeatStartStack(seatNo) - hand.GetSeatBetAmount(seatNo) + hand.GetSeatWinAmount(seatNo);

    internal static Street GetCurrentStreet(this Hand hand)
        => hand.GetLastAction().Street;

    internal static decimal GetCurrentStreetPot(this Hand hand)
        => hand.PlayerStartStates.Select(pss =>
        {
            var seatLastAction = hand.GetSeatLastAction(pss.SeatNo);
            return seatLastAction?.Street == hand.GetCurrentStreet() ? seatLastAction.Amount : 0;
        })
        .Sum();

    internal static decimal GetPot(this Hand hand)
        => hand.PlayerStartStates.Select(pss => hand.GetSeatLastAction(pss.SeatNo)?.Amount ?? 0)
        .Sum();

    internal static bool IsSeatFolded(this Hand hand, byte seatNo)
        => hand.GetSeatActions(seatNo).SelectMany(v => v.Value).Any(act => act.PlayAction == PlayerActionType.Fold);

    internal static bool IsSeatDidActions(this Hand hand, byte seatNo)
        => hand.GetSeatActions(seatNo).SelectMany(v => v.Value).Any();

    internal static int GetPlayerIx(this Player player)
        => player.SeatNo - 1;
}
