using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Options;
public class HoldemEngineOptions
{
    public IDbContextFactory<HoldemDbContext> DbContextFactory { get; internal set; } = null!;
}
