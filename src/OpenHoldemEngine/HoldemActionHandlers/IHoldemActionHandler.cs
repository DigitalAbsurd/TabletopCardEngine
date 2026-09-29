using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal interface IHoldemActionHandler
{
    Task<HoldemError?> HandleAsync(HoldemAction holdemAction, Game game);

    static IHoldemActionHandler CreateHandlerForAction(HoldemAction holdemAction, HoldemEngineOptions options)
    => holdemAction switch
    {
        HoldemActionCreateGame => new CreateGameActionHandler(options.Settings),
        HoldemActionGetGame => new GetGameActionHandler(),
        HoldemActionStartGame => new StartGameActionHandler(options.CardShuffler),
        HoldemActionFinishGame => new FinishGameActionHandler(),
        HoldemActionStartHand => new StartHandActionHandler(options.CardShuffler),
        HoldemActionPlayerAction => new PlayerActionHandler(options.CardShuffler),
        _ => new UnknownActionHandler()
    };
}
