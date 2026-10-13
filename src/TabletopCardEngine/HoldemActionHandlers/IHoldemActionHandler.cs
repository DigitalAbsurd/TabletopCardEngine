using TabletopCardEngine.Data;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Options;

namespace TabletopCardEngine.HoldemActionHandlers;

internal interface IHoldemActionHandler
{
    Task<HoldemError?> HandleAsync(HoldemAction holdemAction, Game game, CancellationToken cancellationToken);

    static IHoldemActionHandler CreateHandlerForAction(HoldemAction holdemAction, HoldemEngineOptions options)
    => holdemAction switch
    {
        HoldemActionCreateGame => new CreateGameActionHandler(options.Settings),
        HoldemActionGetGame => new GetGameActionHandler(),
        HoldemActionStartGame => new StartGameActionHandler(options.CardShuffler),
        HoldemActionFinishGame => new FinishGameActionHandler(),
        HoldemActionStartHand => new StartHandActionHandler(options.CardShuffler),
        HoldemActionPlay => new PlayActionHandler(options.CardShuffler),
        _ => new UnknownActionHandler()
    };
}
