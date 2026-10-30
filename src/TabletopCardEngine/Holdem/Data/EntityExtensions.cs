using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.Data;

internal static class EntityExtensions
{
    internal static Hand GetCurrentHand(this Game game)
        => game.Hands.LastOrDefault() ?? new Hand();

    internal static Hand GetPreviousHand(this Game game)
        => game.Hands.SingleOrDefault(hand => hand.No == game.GetCurrentHand().No - 1) ?? new Hand();

    internal static HandAction GetLastAction(this Hand hand)
        => hand.Actions.LastOrDefault() ?? new HandAction();

    internal static List<Card> GetDeckCards(this Hand hand)
        => [.. hand.DeckCards
            .OrderBy(deckCard => deckCard.Sequence)
            .Select(deckCard => deckCard.Card)];

    internal static Street GetStreet(this Hand hand)
        => hand.GetLastAction().NextStreet;

    internal static bool IsStarted(this Hand hand)
        => hand.Actions.Count != 0;

    internal static bool TryGetBlindSeats(this Hand hand, out byte smallBlindSeatNo, out byte bigBlindSeatNo)
    {
        if (!hand.IsStarted())
        {
            smallBlindSeatNo = 0;
            bigBlindSeatNo = 0;
            return false;
        }

        smallBlindSeatNo = hand.Actions.SingleOrDefault(action => action.ActionType == HandActionType.SmallBlind)?.SeatNo
            ?? throw new ArgumentException("The hand has started without a small blind.", nameof(smallBlindSeatNo));
        bigBlindSeatNo = hand.Actions.SingleOrDefault(action => action.ActionType == HandActionType.BigBlind)?.SeatNo
            ?? throw new ArgumentException("The hand has started without a big blind.", nameof(bigBlindSeatNo));

        return true;
    }

    internal static Card[]? GetSeatHoleCards(this Hand hand, byte seatNo)
        => hand.HoleCards.Any(holeCard => holeCard.SeatNo == seatNo)
            ? [.. hand.HoleCards.Where(holeCard => holeCard.SeatNo == seatNo).Select(holeCard => holeCard.Card)]
            : null;

    internal static decimal GetSeatStartStack(this Hand hand, byte seatNo)
        => hand.PlayerStartStates.Single(startState => startState.SeatNo == seatNo).StartStack;

    internal static decimal GetSeatCurrentStack(this Hand hand, byte seatNo)
        => hand.GetSeatStartStack(seatNo) - hand.GetSeatBetAmount(seatNo);

    internal static decimal GetSeatWinAmount(this Hand hand, byte seatNo)
        => hand.PlayerEndStates.SingleOrDefault(endState => endState.SeatNo == seatNo)?.WinAmount ?? 0;

    internal static decimal GetSeatBetAmount(this Hand hand, byte seatNo)
        => hand.GetSeatActions(seatNo)
            .Select(street => street.Value.Max(action => action.Amount))
            .Sum();

    internal static Dictionary<Street, List<HandAction>> GetSeatActions(this Hand hand, byte seatNo)
        => hand.Actions
            .Where(action => action.SeatNo == seatNo)
            .OrderBy(action => action.No)
            .GroupBy(action => action.Street)
            .ToDictionary(street => street.Key, street => street.ToList());

    internal static HandAction? GetSeatLastAction(this Hand hand, byte seatNo)
        => hand.Actions
            .Where(action => action.SeatNo == seatNo)
            .LastOrDefault();

    internal static decimal GetSeatActualStack(this Hand hand, byte seatNo)
        => hand.GetSeatStartStack(seatNo) - hand.GetSeatBetAmount(seatNo) + hand.GetSeatWinAmount(seatNo);

    internal static decimal GetSeatFinishStack(this Hand hand, byte seatNo)
        => hand.PlayerEndStates.SingleOrDefault(endState => endState.SeatNo == seatNo)?.FinishStack ?? 0;

    internal static Street GetCurrentStreet(this Hand hand)
        => hand.GetLastAction().Street;

    internal static decimal GetCurrentStreetPot(this Hand hand)
        => hand.PlayerStartStates.Select(startState =>
        {
            var seatLastAction = hand.GetSeatLastAction(startState.SeatNo);
            return seatLastAction?.Street == hand.GetCurrentStreet() ? seatLastAction.Amount : 0;
        })
        .Sum();

    internal static decimal GetPot(this Hand hand)
        => hand.PlayerStartStates.Select(startState => hand.GetSeatLastAction(startState.SeatNo)?.Amount ?? 0)
        .Sum();

    internal static bool IsSeatFolded(this Hand hand, byte seatNo)
        => hand.GetSeatActions(seatNo).SelectMany(street => street.Value).Any(action => action.ActionType == HandActionType.Fold);

    internal static bool HasSeatActed(this Hand hand, byte seatNo)
        => hand.GetSeatActions(seatNo).SelectMany(street => street.Value).Any();

    /// <summary>
    /// The seat's latest occupant: its <see cref="Player"/> row with the highest <see cref="Player.HandNoStart"/>,
    /// whether or not that player has left since, or <see langword="null"/> for a seat nobody has taken.
    /// </summary>
    internal static Player? GetSeatLatestPlayer(this Game game, byte seatNo)
        => game.Players
            .Where(player => player.SeatNo == seatNo)
            .OrderByDescending(player => player.HandNoStart)
            .FirstOrDefault();

    /// <summary>Whether the seat has a player who has not left: the seat is taken for the next hand.</summary>
    internal static bool IsSeatTaken(this Game game, byte seatNo)
        => game.GetSeatLatestPlayer(seatNo) is { HandNoEnd: null };

    /// <summary>
    /// The player who sat in the seat during hand <paramref name="handNo"/>, or <see langword="null"/>. A seat has
    /// one row per occupant, so this is the row that had joined by then and had not left before it.
    /// </summary>
    internal static Player? GetSeatPlayer(this Game game, byte seatNo, int handNo)
        => game.Players
            .Where(player => player.SeatNo == seatNo && player.HandNoStart <= handNo && (player.HandNoEnd == null || player.HandNoEnd >= handNo))
            .OrderByDescending(player => player.HandNoStart)
            .FirstOrDefault();

    /// <summary>
    /// The players of <paramref name="hand"/>, one per seat that has a start state in it, ascending by seat number.
    /// </summary>
    internal static IEnumerable<Player> GetHandPlayers(this Game game, Hand hand)
        => hand.PlayerStartStates
            .Select(startState => game.GetSeatPlayer(startState.SeatNo, hand.No))
            .OfType<Player>()
            .OrderBy(player => player.SeatNo);

    internal static int GetPlayerIndex(this Player player)
        => player.SeatNo - 1;

    internal static int GetNextHandNo(this Game game)
        => game.GetCurrentHand().No + 1;

    internal static void JoinPlayers(this Game game, IReadOnlyList<PlayerJoinData> playerJoins)
    {
        var nextHandNo = game.GetNextHandNo();

        game.Players.AddRange(playerJoins.Select(
            seat => new Player
            {
                Name = seat.Name,
                SeatNo = seat.SeatNo,
                HandNoStart = nextHandNo
            }));

        game.BuyIns.AddRange(playerJoins
            .Select(join => new BuyIn
            {
                HandNo = nextHandNo,
                SeatNo = join.SeatNo,
                Amount = join.BuyInAmount
            }));
    }

    internal static void CreateNewHand(this Game game, IEnumerable<HandPlayerStartState> startStacks)
        => game.Hands.Add(new Hand
        {
            No = game.GetNextHandNo(),
            GameId = game.Id,
            PlayerStartStates = [.. startStacks],
        });

    internal static void AddHoleCards(this Game game, byte seatNo, IEnumerable<Card> hole)
        => hole.Select((card, index) => new { Card = card, Sequence = (byte)(index + 1) })
            .ToList()
            .ForEach(holeCard => game.GetCurrentHand().HoleCards
                .Add(new HandHoleCard { Card = holeCard.Card, SeatNo = seatNo, Sequence = holeCard.Sequence }));

    internal static HandAction AddAction(this Game game, byte seatNo, Street street, HandActionType handActionType,
        decimal size, decimal amount, decimal addAmount, byte nextSeatNo, Street nextStreet, decimal paid = default)
    {
        var handAction = new HandAction
        {
            No = game.GetCurrentHand().Actions.DefaultIfEmpty(new HandAction()).Max(action => action.No) + 1,
            ActionType = handActionType,
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
            Sequence = (byte)(game.GetCurrentHand().DeckCards.DefaultIfEmpty(new HandDeckCard()).Max(deckCard => deckCard.Sequence) + 1),
        });
    }

    internal static void AddHandEndStates(this Game game, IEnumerable<HandPlayerEndState> handPlayerEndStates)
        => game.GetCurrentHand().PlayerEndStates.AddRange(handPlayerEndStates);

    internal static bool IsCurrentHandEnded(this Game game)
        => game.GetCurrentHand().No == 0 || game.GetCurrentHand().PlayerEndStates.Count != 0;
}
