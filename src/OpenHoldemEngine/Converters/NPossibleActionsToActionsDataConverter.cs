global using NAmountInterval = NPokerEngine.Types.AmountInterval;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.Converters;
internal class NPossibleActionsToActionsDataConverter :
    ValueConverter<IEnumerable<(NPokerActionType nPokerActionType, NAmountInterval nAmountInterval)>?, PlayerActionEvaluationData[]>
{
    private static readonly HandActionTypeToNPokerTypeConverter _actionTypeToNPokerTypeConverter = new();

    public NPossibleActionsToActionsDataConverter()
        : base(nPokerPossibleActions => NPokerEvaluationsToPossibleActions(nPokerPossibleActions),
            evaluations => null!)
    { }

    private static PlayerActionEvaluationData[] NPokerEvaluationsToPossibleActions
        (IEnumerable<(NPokerActionType nPokerActionType, NAmountInterval nAmountInterval)>? nPokerPossibleActions)
    {
        if (nPokerPossibleActions == null)
            return Array.Empty<PlayerActionEvaluationData>();

        return nPokerPossibleActions.Select(t =>
            new PlayerActionEvaluationData(_actionTypeToNPokerTypeConverter.ConvertFrom<PlayerActionType>(t.nPokerActionType),
                (decimal)t.nAmountInterval.Value,
                t.nAmountInterval.MaxValue.HasValue ? (decimal)t.nAmountInterval.MaxValue.Value : 0M))
            .ToArray();
    }
}
