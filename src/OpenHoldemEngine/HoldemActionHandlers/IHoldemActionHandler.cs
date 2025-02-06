using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal interface IHoldemActionHandler
{
    Task<Result> HandleAsync(HoldemAction holdemAction, Game game);

    public static IHoldemActionHandler CreateHandlerForAction(HoldemAction holdemAction)
    => holdemAction switch
    {
        HoldemActionCreateGame => new CreateGameActionHandler(),
        HoldemActionGetGame => new GetGameActionHandler(),
        HoldemActionStartHand => new StartHandActionHandler(),
        HoldemActionPlayerAction => new PlayerActionHandler(),
        _ => throw new NotImplementedException()
    };
}
