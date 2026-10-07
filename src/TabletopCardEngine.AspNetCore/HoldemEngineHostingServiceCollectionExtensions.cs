using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TabletopCardEngine.AspNetCore.Json;
using TabletopCardEngine.Options;

namespace TabletopCardEngine.AspNetCore;

/// <summary>Registers the engine in an ASP.NET Core host, with its settings bound from configuration.</summary>
public static class HoldemEngineHostingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the engine like <see cref="ServiceCollectionExtensions.AddHoldemEngine(IServiceCollection, Action{DbContextOptionsBuilder}, Action{HoldemEngineOptionsBuilder}?)"/>,
    /// and takes its settings from a configuration section bound to <see cref="HoldemEngineHostOptions"/>.
    /// <para>
    /// The database stays in code: the package references no provider, so the host passes
    /// <paramref name="dbOptionsAction"/> as before.
    /// </para>
    /// <para>
    /// Precedence: the configuration section is the source of the settings, and it overwrites a
    /// <c>WithSettings</c> call made inside <paramref name="configure"/>. Use <paramref name="configure"/>
    /// for <c>WithCardShuffler</c>.
    /// </para>
    /// <para>
    /// The section is validated with data annotations when the host starts, so a bad value fails
    /// <c>StartAsync</c> with an <see cref="OptionsValidationException"/> before any request.
    /// </para>
    /// </summary>
    /// <param name="services">The container to register into.</param>
    /// <param name="configuration">The section holding the <see cref="HoldemEngineHostOptions"/> keys.</param>
    /// <param name="dbOptionsAction">Configures the database provider, e.g. <c>db =&gt; db.UseHoldemSqlite(...)</c>.</param>
    /// <param name="configure">Configures the optional card shuffler; its settings are overwritten by the section.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddHoldemEngine(this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> dbOptionsAction,
        Action<HoldemEngineOptionsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dbOptionsAction);

        services.AddOptions<HoldemEngineHostOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        ServiceCollectionExtensions.AddHoldemEngine(services, dbOptionsAction, configure);

        services.ConfigureHttpJsonOptions(o => HoldemJson.AddConverters(o.SerializerOptions.Converters));

        services.AddSingleton<HoldemEngineApiMarker>();

        // Keep the instance the core's configure already wrote into (the card shuffler), and apply
        // the bound settings to it when the engine's options are first resolved.
        var descriptor = services.Single(d => d.ServiceType == typeof(HoldemEngineOptions));
        var instance = (HoldemEngineOptions)descriptor.ImplementationInstance!;
        services.Replace(ServiceDescriptor.Singleton(sp =>
        {
            var bound = sp.GetRequiredService<IOptions<HoldemEngineHostOptions>>().Value;
            new HoldemEngineOptionsBuilder(instance).WithSettings(s =>
            {
                s.GamePlayersMinCount = bound.GamePlayersMinCount;
                s.GamePlayersMaxCount = bound.GamePlayersMaxCount;
                s.EnableSensitiveCardLogging = bound.EnableSensitiveCardLogging;
            });
            return instance;
        }));

        services.AddHostedService<HoldemSchemaMigrationHostedService>();

        return services;
    }
}
