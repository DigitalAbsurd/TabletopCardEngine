using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.Data;
internal static class EntityExtensions
{
    internal static Hand GetCurrentHand(this Game game)
        => game.Hands.LastOrDefault() ?? new Hand();

    internal static Hand GetPreviousHand(this Game game)
        => game.Hands.SingleOrDefault(h => h.No == game.GetCurrentHand().No - 1) ?? new Hand();

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

    internal static decimal GetSeatFinishStack(this Hand hand, byte seatNo)
        => hand.PlayerEndStates.SingleOrDefault(est => est.SeatNo == seatNo)?.FinishStack ?? 0;

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

    internal static int GetNextHandNo(this Game game)
        => game.GetCurrentHand().No + 1;

    internal static void JoinPlayers(this Game game, PlayerJoinData[] playerJoins)
    {
        var nextHandNo = game.GetNextHandNo();

        game.Players.AddRange(playerJoins.Select(
            seat => new Player
            {
                Name = seat.Name,
                SeatNo = seat.SeatNo,
                RoundNoStart = nextHandNo
            }));

        game.BuyIns.AddRange(playerJoins.
            Select(b => new BuyIn
            {
                HandNo = nextHandNo,
                SeatNo = b.SeatNo,
                Amount = b.BuyInAmount
            }));
    }

    internal static void CreateNewHand(this Game game, IEnumerable<HandPlayerStartState> startStacks)
        => game.Hands.Add(new Hand
        {
            No = game.GetNextHandNo(),
            GameId = game.Id,
            PlayerStartStates = startStacks.ToList(),
        });

    internal static void AddHoleCards(this Game game, byte seatNo, IEnumerable<Card> hole)
        => hole.Select((card, ix) => new { Card = card, SeqNo = (byte)(ix + 1) })
            .ToList()
            .ForEach(hc => game.GetCurrentHand().HoleCards.Add(new HandHoleCard { Card = hc.Card, SeatNo = seatNo, Sequence = hc.SeqNo }));

    internal static HandAction AddAction(this Game game, byte seatNo, Street street, PlayerActionType playerActionType,
        decimal size, decimal amount, decimal addAmount, byte nextSeatNo, Street nextStreet, decimal paid = default)
    {
        var handAction = new HandAction
        {
            No = game.GetCurrentHand().Actions.DefaultIfEmpty(new HandAction()).Max(x => x.No) + 1,
            PlayAction = playerActionType,
            SeatNo = seatNo,
            Amount = amount,
            Size = size,
            AddAmount = addAmount,
            NextSeatNo = nextSeatNo,
            Street = street,
            NextStreet = nextStreet,
            Paid = paid
        };
        game.GetCurrentHand().Actions.Add(handAction);
        return handAction;
    }

    internal static void AddDeckCard(this Game game, Card card)
    {
        game.GetCurrentHand().DeckCards.Add(new HandDeckCard
        {
            Card = card,
            Sequence = (byte)(game.GetCurrentHand().DeckCards.DefaultIfEmpty(new HandDeckCard()).Max(x => x.Sequence) + 1),
        });
    }

    internal static void AddHandEndStates(this Game game, IEnumerable<HandPlayerEndState> handPlayerEndStates)
        => game.GetCurrentHand().PlayerEndStates.AddRange(handPlayerEndStates);

    internal static NPokerEmulator GetEmulator(this Game game)
    {
        var nEmulator = new NPokerEngine.Emulator();
        nEmulator.SetupConfig(int.MaxValue, int.MaxValue, (float)game.InitialSmallBlind);
        return nEmulator;
    }
}
