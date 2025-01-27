using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.DTO;

#region PRIMITIVES
public record GameOptionData(int MaxPlayersCount = 6, decimal InitialSmallBlind = 1, byte InitialDealerSeatNo = 1, bool AllowRebuy = false, int MaxHandsCount = 0, int AutoIncrementsHands = 0);
public record PlayerJoinData(string Name, byte SeatNo, decimal BuyInAmount);
public record BuyInData(byte SeatNo, decimal Amount);
public record PlayerActionEvaluationData(PlayerActionType PlayerActionType, decimal MinValue = 0, decimal MaxValue = 0);
public record HandResultData(bool IsWinner, decimal WinSize, decimal Net, string HandTypeCode, int HandStrength, string HandDescription, bool Folded = false);
#endregion

#region CONTEXTS
public record PlayerActionData(Street Street, PlayerActionType ActionType, decimal Size, int No, string DisplayValue);
public record SeatData(byte SeatNo, string Name, decimal StartRoundStack,
    decimal Stack, decimal? BetSize = null, Card[]? Hole = null,
    PlayerActionData? LastAction = null, HandResultData? RoundResultData = null)
{
    const decimal DECIMAL_MIN_PRECISION = 0.0001M;
    public bool IsActive => (Stack > DECIMAL_MIN_PRECISION && (Hole?.Any() == true && LastAction?.ActionType != PlayerActionType.Fold)) ||
    (Stack < DECIMAL_MIN_PRECISION && LastAction != null && LastAction.ActionType != PlayerActionType.Fold);
    public bool IsFolded => LastAction?.ActionType == PlayerActionType.Fold;
    public bool IsAllIn => LastAction != default && Stack < DECIMAL_MIN_PRECISION;
};
public record HandData(int No, SeatData[] Seats, byte DealerSeatNo, byte SmallBlindSeatNo, byte BigBlindSeatNo, byte NextPlayerSeatNo, Street Street, decimal StreetPot, decimal Pot, Card[] CommunityCards, PlayerActionEvaluationData[] Evaluations)
{
    SeatData? DealerRef => Seats.SingleOrDefault(p => p.SeatNo == DealerSeatNo);
    SeatData? SmallBlindRef => Seats.SingleOrDefault(p => p.SeatNo == SmallBlindSeatNo);
    SeatData? BigBlindRef => Seats.SingleOrDefault(p => p.SeatNo == BigBlindSeatNo);
    SeatData? NextPlayerRef => Seats.SingleOrDefault(p => p.SeatNo == NextPlayerSeatNo);
    internal decimal GetEvalMinValue(PlayerActionType actionType) => Evaluations.Single(ev => ev.PlayerActionType == actionType).MinValue;
    internal decimal GetEvalMaхValue(PlayerActionType actionType) => Evaluations.Single(ev => ev.PlayerActionType == actionType).MaxValue;
    public bool IsFinishedWithShowdown => Street == Street.Finished && Seats.Where(p => p.RoundResultData != null && !p.RoundResultData.Folded && p.LastAction != null && p.LastAction != null).Count() > 1;
};
public record GameData(Guid GameId, GameOptionData Options, HandData HandData);
#endregion

#region HOLDEM_ACTIONS
public abstract record HoldemAction(Guid GameId);
public record HoldemActionCreateGame(Guid GameId, GameOptionData Options) : HoldemAction(GameId);
public record HoldemActionStartGame(Guid GameId, PlayerJoinData[] JoinSeats) : HoldemAction(GameId);
public record HoldemActionStartHand(Guid GameId) : HoldemAction(GameId)
{
    public PlayerJoinData[] JoinSeats { get; set; } = [];
    public BuyInData[] BuyIns { get; set; } = [];
    public byte[] KickOffSeats { get; set; } = [];
};
public record HoldemActionPlayerAction(Guid GameId, PlayerActionType ActionType, decimal Size) : HoldemAction(GameId);
public record HoldemActionFinishGame(Guid GameId) : HoldemAction(GameId);
#endregion