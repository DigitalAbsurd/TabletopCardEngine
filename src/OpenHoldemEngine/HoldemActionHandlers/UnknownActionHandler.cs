using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

/// <summary>Answers an action record the engine has no handler for, so that a caller's own subclass fails cleanly instead of throwing.</summary>
internal sealed class UnknownActionHandler : IHoldemActionHandler
{
    public Task<HoldemError?> HandleAsync(HoldemAction holdemAction, Game game)
        => Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.InvalidAction, PackResource.HoldemValidation_InvalidAction));
}
