using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;

public class HoldemEngine(HoldemEngineOptions _options)
{
    public Task<Guid> GetGameContextAsync(Guid gameId)
    {
        throw new NotImplementedException();
    }
}