using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.Metrics;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Diagnostics;
using TabletopCardEngine.Holdem.Options;

namespace TabletopCardEngine.Holdem;

/// <summary>Adds Texas Hold'em to the engine's registration.</summary>
public static class HoldemBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IHoldemTable"/> as a singleton over an EF Core context factory.
    /// <para>
    /// Engine options belong to the container: each call creates its own
    /// <see cref="HoldemOptions"/> and registers it as a singleton instance, so engines built
    /// from different containers can be configured differently side by side in one process.
    /// A container holds one Hold'em registration, so a second call on the same collection throws,
    /// whether it is made through the same builder or through a second <c>AddTabletopCardEngine</c> call.
    /// </para>
    /// <para>
    /// The engine does not create its own tables. The schema is created with
    /// <see cref="IHoldemSchemaManager.MigrateAsync"/>, which this call registers per container as
    /// well, over the same database configuration.
    /// </para>
    /// </summary>
    /// <param name="builder">The engine's registration.</param>
    /// <param name="dbOptionsAction">Configures the database provider, e.g. <c>db =&gt; db.UseHoldemSqlite(...)</c>.</param>
    /// <param name="configure">Configures engine settings and the optional <see cref="ICardShuffler"/>.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="InvalidOperationException">Hold'em is already registered in this collection.</exception>
    public static TabletopCardEngineBuilder AddHoldem(this TabletopCardEngineBuilder builder,
        Action<DbContextOptionsBuilder> dbOptionsAction,
        Action<HoldemOptionsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;
        if (services.Any(d => d.ServiceType == typeof(HoldemOptions)))
        {
            throw new InvalidOperationException(
                "Hold'em is already registered in this service collection; call AddHoldem once per container.");
        }

        var options = new HoldemOptions();
        services.AddDbContextFactory<HoldemDbContext>(opts =>
        {
            dbOptionsAction?.Invoke(opts);
            opts.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });

        configure?.Invoke(new HoldemOptionsBuilder(options));
        services.AddSingleton(options);
        services.AddSingleton<HoldemSchemaManager>(sp =>
            new HoldemSchemaManager(sp.GetRequiredService<IDbContextFactory<HoldemDbContext>>()));
        services.AddSingleton<IHoldemSchemaManager>(sp =>
            sp.GetRequiredService<HoldemSchemaManager>());
        services.AddSingleton<IHoldemTable>(servicesProvider =>
        {
            var resolvedOptions = servicesProvider.GetRequiredService<HoldemOptions>();
            resolvedOptions.DbContextFactory =
                servicesProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();
            var logger = servicesProvider.GetService<ILoggerFactory>()?.CreateLogger<HoldemTable>()
                ?? NullLogger<HoldemTable>.Instance;
            var meterFactory = servicesProvider.GetService<IMeterFactory>();
            var metrics = HoldemMetrics.CreateOrGetFallback(meterFactory);
            var schemaManager = servicesProvider.GetRequiredService<HoldemSchemaManager>();
            return new HoldemTable(resolvedOptions, logger, metrics, schemaManager);
        });

        return builder;
    }
}
