using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.DTO;

#region PRIMITIVES
public record GameOptions(decimal InitialStack = 100, decimal InitialSmallBlind = 1, bool AllowRebuy = false, byte StartDealerSeatNo = 1);
public record PlayerDefinition(string Name, byte SeatNo, int RoundNoStart = 0);
public record PlayerActionEvaluation(PlayerActionType PlayerActionType, decimal MinValue = 0, decimal MaxValue = 0);
public record HandResult(bool IsWinner, decimal WinSize, decimal Net, string HandTypeCode, int HandStrength, string HandDescription, bool Folded = false);
public record ActionEvaluation(PlayerActionType PlayerActionType, decimal MinValue = 0, decimal MaxValue = 0);
#endregion

#region CONTEXTS
public record PlayerActionContext(Street Street, PlayerActionType ActionType, decimal Size, int No, string DisplayValue);
public record PlayerContext(PlayerDefinition PlayerDefinition, float StartRoundStack,
    decimal Stack, decimal? BetSize = null, Card[]? Hole = null,
    PlayerActionContext? LastAction = null, HandResult? RoundResultData = null)
{
    const decimal DECIMAL_MIN_PRECISION = 0.0001M;
    public bool IsActive => (Stack > DECIMAL_MIN_PRECISION && (Hole?.Any() == true && LastAction?.ActionType != PlayerActionType.Fold)) ||
    (Stack < DECIMAL_MIN_PRECISION && LastAction != null && LastAction.ActionType != PlayerActionType.Fold);
    public bool IsFolded => LastAction?.ActionType == PlayerActionType.Fold;
    public bool IsAllIn => LastAction != default && Stack < DECIMAL_MIN_PRECISION;
};
public record HandContext(PlayerContext[] Players, byte DealerSeatNo, byte SmallBlindSeatNo, byte BigBlindSeatNo, byte NextPlayerSeatNo, int No, int ActionsCount, Street Street, float Pot, float StreetPot, Card[]? CommunityCards = null)
{
    PlayerContext? DealerRef => Players.SingleOrDefault(p => p.PlayerDefinition.SeatNo == DealerSeatNo);
    PlayerContext? SmallBlindRef => Players.SingleOrDefault(p => p.PlayerDefinition.SeatNo == SmallBlindSeatNo);
    PlayerContext? BigBlindRef => Players.SingleOrDefault(p => p.PlayerDefinition.SeatNo == BigBlindSeatNo);
    PlayerContext? NextPlayerRef => Players.SingleOrDefault(p => p.PlayerDefinition.SeatNo == NextPlayerSeatNo);
    public bool IsFinishedWithShowdown => Street == Street.Finished && Players.Where(p => p.RoundResultData != null && !p.RoundResultData.Folded && p.LastAction != null && p.LastAction != null).Count() > 1;
};
public record GameContext(Guid GameId, HoldemActionDetail ActionDetail, GameOptions? Options = null, PlayerDefinition[]? PlayerDefinitions = null, HandContext? HandContext = null)
{
    public bool IsWaitingForStart => ActionDetail is HoldemActionDetailStartGame;
};
#endregion

#region HOLDEM_ACTION_DETAILS
public abstract record HoldemActionDetail;
public record HoldemActionDetailStartGame(int PlayersCountMin = 2, int PlayersCountMax = 6) : HoldemActionDetail;
public record HoldemActionDetailNewHand(int NextHandNo) : HoldemActionDetail;
public record HoldemActionDetailPlayerAction(PlayerDefinition PlayerDefinition, ActionEvaluation[] Evaluations) : HoldemActionDetail;
public record HoldemActionDetailFinishGame : HoldemActionDetail;
#endregion

#region HOLDEM_ACTIONS
public abstract record HoldemAction(Guid GameId);
public record HoldemActionStartGame(Guid GameId, PlayerDefinition[] PlayerDefinitions, GameOptions Options) : HoldemAction(GameId);
public record HoldemActionNewHand(Guid GameId, List<PlayerDefinition>? KickOffPlayerDefinitions = null, List<PlayerDefinition>? JoinPlayerDefinitions = null, Dictionary<byte, int>? BuyIns = null) : HoldemAction(GameId);
public record HoldemActionPlayerAction(Guid GameId, PlayerActionType ActionType, decimal Size) : HoldemAction(GameId);
public record HoldemActionFinishGame(Guid GameId) : HoldemAction(GameId);
#endregion