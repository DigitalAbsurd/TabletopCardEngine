using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TabletopCardEngine.AspNetCore;

/// <summary>Adds the engine's database check to an ASP.NET Core health checks registration.</summary>
public static class HoldemEngineHealthChecksBuilderExtensions
{
    /// <summary>
    /// Registers a check that reads <see cref="IHoldemSchemaManager.GetSchemaStatusAsync"/> on every
    /// probe, so it reports both whether the database is reachable and whether its schema is one this
    /// engine can serve.
    /// <para>
    /// A schema that needs migrating, or one migrated by a newer engine, reports
    /// <paramref name="failureStatus"/>: the engine refuses every call in either state, so the default
    /// is <see cref="HealthStatus.Unhealthy"/> rather than <see cref="HealthStatus.Degraded"/>. An
    /// unreachable database surfaces as the provider's exception, which the health check service
    /// reports with the same status. The check never migrates the database.
    /// </para>
    /// </summary>
    /// <param name="builder">The health checks builder returned by <c>AddHealthChecks()</c>.</param>
    /// <param name="name">The registration name, which is also the key of the entry in the report.</param>
    /// <param name="failureStatus">The status to report on failure; <c>null</c> means <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">Tags a health endpoint can filter the check by.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthChecksBuilder AddHoldemEngineCheck(this IHealthChecksBuilder builder,
        string name = "tabletopcardengine",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new HoldemSchemaHealthCheck(sp.GetRequiredService<IHoldemSchemaManager>()),
            failureStatus,
            tags));
    }
}
