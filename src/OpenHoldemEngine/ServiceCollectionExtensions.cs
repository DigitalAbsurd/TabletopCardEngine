using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHoldemEngine(this IServiceCollection services,
        Action<DbContextOptionsBuilder>? dbOptionsAction = null,
        Action<HoldemEngineOptionsBuilder>? configure = null)
    {
        var optionsBuilder = new HoldemEngineOptionsBuilder();
        services.AddDbContextFactory<HoldemDbContext>(dbOptionsAction);
        configure?.Invoke(optionsBuilder);
        services.AddSingleton<HoldemEngine>(servicesProvider =>
        {
            optionsBuilder.Options.DbContextFactory = servicesProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();
            return new HoldemEngine(optionsBuilder.Options);
        });
        return services;
    }
}
