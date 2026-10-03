using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OpenHoldemEngine.AspNetCore;

/// <summary>
/// Maps <see cref="HoldemSchemaStatus"/> onto a health result. Exceptions are deliberately not caught:
/// the health check service already reports one as the registration's failure status with the
/// exception attached, which is how an unreachable database and an unsupported provider show up.
/// </summary>
internal sealed class HoldemSchemaHealthCheck(IHoldemSchemaManager schemaManager) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var status = await schemaManager.GetSchemaStatusAsync(cancellationToken).ConfigureAwait(false);

        // The data dictionary takes no nulls, so a value the database does not record is left out.
        var data = new Dictionary<string, object>
        {
            ["state"] = status.State.ToString(),
            ["providerName"] = status.ProviderName,
            ["engineSchemaVersion"] = status.EngineSchemaVersion,
            ["pendingMigrations"] = status.PendingMigrations.Count,
        };
        if (status.DatabaseSchemaVersion is { } databaseSchemaVersion)
        {
            data["databaseSchemaVersion"] = databaseSchemaVersion;
        }
        if (status.DatabaseEngineVersion is { } databaseEngineVersion)
        {
            data["databaseEngineVersion"] = databaseEngineVersion;
        }

        return status.State switch
        {
            HoldemSchemaState.UpToDate => HealthCheckResult.Healthy(
                $"Schema version {status.EngineSchemaVersion} is up to date.", data),
            HoldemSchemaState.NotVersioned => HealthCheckResult.Healthy(
                "The provider keeps no schema version.", data),
            HoldemSchemaState.MigrationRequired => new HealthCheckResult(context.Registration.FailureStatus,
                $"The database schema needs migrating ({status.PendingMigrations.Count} pending migrations).", data: data),
            HoldemSchemaState.NewerThanEngine => new HealthCheckResult(context.Registration.FailureStatus,
                $"The database was migrated by a newer engine (schema {status.DatabaseSchemaVersion}, this engine expects {status.EngineSchemaVersion}).", data: data),
            _ => throw new InvalidOperationException($"Unknown schema state {status.State}."),
        };
    }
}
