using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Diagnostics;
using OpenHoldemEngine.Options;
using System.Diagnostics.Metrics;

namespace OpenHoldemEngine;

/// <summary>Registers the engine and its database context with a dependency-injection container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="HoldemEngine"/> as a singleton over an EF Core context factory.
    /// <para>
    /// Engine options belong to the container: each call creates its own
    /// <see cref="HoldemEngineOptions"/> and registers it as a singleton instance, so engines built
    /// from different containers can be configured differently side by side in one process.
    /// </para>
    /// <para>
    /// The engine does not create its own tables. The schema is created with
    /// <see cref="IHoldemSchemaManager.MigrateAsync"/>, which this call registers per container as
    /// well, over the same database configuration.
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
        var options = new HoldemEngineOptions();
        services.AddDbContextFactory<HoldemDbContext>(opts =>
        {
            dbOptionsAction?.Invoke(opts);
            opts.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });

        configure?.Invoke(new HoldemEngineOptionsBuilder(options));
        services.AddSingleton(options);
        services.AddSingleton<HoldemSchemaManager>(sp =>
            new HoldemSchemaManager(sp.GetRequiredService<IDbContextFactory<HoldemDbContext>>()));
        services.AddSingleton<IHoldemSchemaManager>(sp =>
            sp.GetRequiredService<HoldemSchemaManager>());
        services.AddSingleton<HoldemEngine>(servicesProvider =>
        {
            var resolvedOptions = servicesProvider.GetRequiredService<HoldemEngineOptions>();
            resolvedOptions.DbContextFactory =
                servicesProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();
            var logger = servicesProvider.GetService<ILoggerFactory>()?.CreateLogger<HoldemEngine>()
                ?? NullLogger<HoldemEngine>.Instance;
            var meterFactory = servicesProvider.GetService<IMeterFactory>();
            var metrics = HoldemEngineMetrics.CreateOrGetFallback(meterFactory);
            var schemaManager = servicesProvider.GetRequiredService<HoldemSchemaManager>();
            return new HoldemEngine(resolvedOptions, logger, metrics, schemaManager);
        });

        return services;
    }
}
