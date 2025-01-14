using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Converters;
internal class HandActionTypeToNPokerTypeConverter : ValueConverter<PlayerActionType, NPokerActionType>
{
    public HandActionTypeToNPokerTypeConverter()
        : base(pat => PlayerActionTypeToNPokerActionType(pat),
            nat => NPokerActionTypeToPlayerActionType(nat))
    { }

    private static NPokerActionType PlayerActionTypeToNPokerActionType(PlayerActionType playerActionType)
        => playerActionType switch
        {
            PlayerActionType.SmallBlind => NPokerActionType.SMALL_BLIND,
            PlayerActionType.BigBlind => NPokerActionType.BIG_BLIND,
            PlayerActionType.Fold => NPokerActionType.FOLD,
            PlayerActionType.Call => NPokerActionType.CALL,
            PlayerActionType.Check => NPokerActionType.CALL,
            PlayerActionType.Bet => NPokerActionType.RAISE,
            PlayerActionType.AllIn => NPokerActionType.RAISE,
            _ => throw new NotImplementedException()
        };

    private static PlayerActionType NPokerActionTypeToPlayerActionType(NPokerActionType nPokerActionType)
    => nPokerActionType switch
    {
        NPokerActionType.SMALL_BLIND => PlayerActionType.SmallBlind,
        NPokerActionType.BIG_BLIND => PlayerActionType.BigBlind,
        NPokerActionType.FOLD => PlayerActionType.Fold,
        NPokerActionType.CALL => PlayerActionType.Call,
        NPokerActionType.RAISE => PlayerActionType.Raise,
        _ => throw new NotImplementedException()
    };
}
