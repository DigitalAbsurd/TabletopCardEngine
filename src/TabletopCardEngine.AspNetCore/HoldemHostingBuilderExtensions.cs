using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TabletopCardEngine.AspNetCore.Json;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Options;

namespace TabletopCardEngine.AspNetCore;

/// <summary>Registers Hold'em in an ASP.NET Core host, with its settings bound from configuration.</summary>
public static class HoldemHostingBuilderExtensions
{
    /// <summary>
    /// Registers Hold'em like the core <c>tce.AddHoldem(dbOptionsAction, configure)</c>, and takes its settings
    /// from a configuration section bound to <see cref="HoldemHostOptions"/>, such as <c>TabletopCardEngine:Holdem</c>.
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
    /// <param name="builder">The engine's registration.</param>
    /// <param name="configuration">The section holding the <see cref="HoldemHostOptions"/> keys.</param>
    /// <param name="dbOptionsAction">Configures the database provider, e.g. <c>db =&gt; db.UseHoldemSqlite(...)</c>.</param>
    /// <param name="configure">Configures the optional card shuffler; its settings are overwritten by the section.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="InvalidOperationException">Hold'em is already registered in this collection.</exception>
    public static TabletopCardEngineBuilder AddHoldem(this TabletopCardEngineBuilder builder,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> dbOptionsAction,
        Action<HoldemOptionsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dbOptionsAction);

        var services = builder.Services;
        services.AddOptions<HoldemHostOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.AddHoldem(dbOptionsAction, configure);

        services.ConfigureHttpJsonOptions(o => HoldemJson.AddConverters(o.SerializerOptions.Converters));

        services.AddSingleton<HoldemApiMarker>();

        // Keep the instance the core's configure already wrote into (the card shuffler), and apply
        // the bound settings to it when the engine's options are first resolved.
        var descriptor = services.Single(d => d.ServiceType == typeof(HoldemOptions));
        var instance = (HoldemOptions)descriptor.ImplementationInstance!;
        services.Replace(ServiceDescriptor.Singleton(sp =>
        {
            var bound = sp.GetRequiredService<IOptions<HoldemHostOptions>>().Value;
            new HoldemOptionsBuilder(instance).WithSettings(s =>
            {
                s.GamePlayersMinCount = bound.GamePlayersMinCount;
                s.GamePlayersMaxCount = bound.GamePlayersMaxCount;
                s.EnableSensitiveCardLogging = bound.EnableSensitiveCardLogging;
            });
            return instance;
        }));

        services.AddHostedService<HoldemSchemaMigrationHostedService>();

        return builder;
    }
}
