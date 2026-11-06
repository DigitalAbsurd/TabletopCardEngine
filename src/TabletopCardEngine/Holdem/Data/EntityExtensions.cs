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

    /// <summary>
    /// The player seated at <paramref name="seatNo"/> who has not left, or <see langword="null"/> for a seat outside the
    /// table, an empty seat, or one whose player has left already: the seats <c>LeaveSeat</c> and <c>KickSeat</c> may vacate.
    /// </summary>
    internal static Player? GetSeatedPlayer(this Game game, byte seatNo)
        => seatNo >= 1 && seatNo <= game.MaxPlayersCount && game.GetSeatLatestPlayer(seatNo) is { HandNoEnd: null } player
            ? player
            : null;

    /// <summary>
    /// Vacates <paramref name="player"/>'s seat after the current hand, for <c>LeaveSeat</c> and <c>KickSeat</c> alike. The
    /// <see cref="Player"/> row is kept and closed with the current hand as its last one, because past hands' start states
    /// and actions still refer to the seat; the next hand is then dealt without it, and the seat is free for a new row.
    /// </summary>
    internal static void LeaveSeat(this Game game, Player player)
    {
        // The current hand is the player's last one, not the next.
        player.HandNoEnd = game.GetCurrentHand().No;

        // Chips topped up for the next hand were never in play, so they leave with the player as if never added.
        if (game.GetSeatPendingBuyIn(player.SeatNo) is { } pending)
        {
            game.BuyIns.Remove(pending);
        }
    }

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

    /// <summary>
    /// The seat's top-up or rebuy waiting for the next hand, or <see langword="null"/>. There is at most one, because
    /// the key is per seat per hand and a second call between the same two hands adds to it.
    /// </summary>
    internal static BuyIn? GetSeatPendingBuyIn(this Game game, byte seatNo)
        => game.BuyIns.SingleOrDefault(buyIn => buyIn.SeatNo == seatNo && buyIn.HandNo == game.GetNextHandNo() && buyIn.Kind != BuyInKind.Join);

    /// <summary>
    /// Every buy-in row for a hand not dealt yet. Those chips were never in play, so they may be cancelled; the rows of
    /// hands already dealt are history and never change.
    /// </summary>
    internal static List<BuyIn> GetPendingBuyIns(this Game game)
        => [.. game.BuyIns.Where(buyIn => buyIn.HandNo > game.GetCurrentHand().No)];

    /// <summary>
    /// How many rebuys <paramref name="player"/> has made, the pending one included. Counted per occupant, so a new
    /// player in the same seat starts from zero: only rows after the hand the player joined at are theirs.
    /// </summary>
    internal static int CountPlayerRebuys(this Game game, Player player)
        => game.BuyIns.Count(buyIn => buyIn.SeatNo == player.SeatNo && buyIn.Kind == BuyInKind.Rebuy && buyIn.HandNo > player.HandNoStart);

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
                Amount = join.BuyInAmount,
                Kind = BuyInKind.Join
            }));
    }

    internal static void CreateNewHand(this Game game, IEnumerable<HandPlayerStartState> startStacks)
    {
        var handNo = game.GetNextHandNo();
        game.Hands.Add(new Hand
        {
            No = handNo,
            GameId = game.Id,
            PlayerStartStates = [.. startStacks],
            SmallBlind = game.InitialSmallBlind,
            BlindLevel = 0,
            BlindLevelStartHandNo = handNo,
            BlindThresholdsPassed = 0,
        });
    }

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
