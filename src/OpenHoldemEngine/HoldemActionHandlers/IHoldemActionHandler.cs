using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal interface IHoldemActionHandler
{
    Task<Result> HandleAsync(HoldemAction holdemAction, Game game);

    static IHoldemActionHandler CreateHandlerForAction(HoldemAction holdemAction)
    => holdemAction switch
    {
        HoldemActionCreateGame => new CreateGameActionHandler(),
        HoldemActionGetGame => new GetGameActionHandler(),
        HoldemActionStartHand => new StartHandActionHandler(),
        HoldemActionPlayerAction => new PlayerActionHandler(),
        _ => throw new NotImplementedException()
    };
}
