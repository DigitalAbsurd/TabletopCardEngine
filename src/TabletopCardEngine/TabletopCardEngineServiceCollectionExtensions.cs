using Microsoft.Extensions.DependencyInjection;

namespace TabletopCardEngine;

/// <summary>Registers the engine with a dependency-injection container.</summary>
public static class TabletopCardEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers the engine, naming the engine first and each game second:
    /// <c>services.AddTabletopCardEngine(tce =&gt; tce.AddHoldem(...))</c>.
    /// <para>
    /// The call registers nothing on its own. Every game is added by its own <c>AddXxx</c> extension
    /// method on <see cref="TabletopCardEngineBuilder"/>, so a container without one has no game service.
    /// </para>
    /// </summary>
    /// <param name="services">The container to register into.</param>
    /// <param name="configure">Adds the games, e.g. <c>tce =&gt; tce.AddHoldem(db =&gt; db.UseHoldemSqlite(...))</c>.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddTabletopCardEngine(this IServiceCollection services,
        Action<TabletopCardEngineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        configure(new TabletopCardEngineBuilder(services));
        return services;
    }
}
