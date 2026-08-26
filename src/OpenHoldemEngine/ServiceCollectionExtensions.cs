using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;

/// <summary>Registers the engine and its database context with a dependency-injection container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="HoldemEngine"/> as a singleton over an EF Core context factory.
    /// <para>
    /// Note that engine options are process-wide: configuring one engine changes them for every
    /// engine in the process.
    /// </para>
    /// </summary>
    /// <param name="services">The container to register into.</param>
    /// <param name="dbOptionsAction">Configures the database provider, e.g. <c>opts =&gt; opts.UseSqlite(...)</c>.</param>
    /// <param name="configure">Configures engine settings and the optional <see cref="ICardShuffler"/>.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddHoldemEngine(this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbOptionsAction,
        Action<HoldemEngineOptionsBuilder>? configure = null)
    {
        var optionsBuilder = new HoldemEngineOptionsBuilder();
        services.AddDbContextFactory<HoldemDbContext>(opts =>
        {
            dbOptionsAction?.Invoke(opts);
            opts.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });

        configure?.Invoke(optionsBuilder);
        services.AddSingleton<HoldemEngine>(servicesProvider =>
        {
            HoldemEngineOptionsBuilder.Options.DbContextFactory =
                servicesProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();
            return new HoldemEngine(HoldemEngineOptionsBuilder.Options);
        });

        return services;
    }
}
