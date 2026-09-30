using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Mappers;

namespace OpenHoldemEngine.Data;

/// <summary>
/// Creates one context per call and disposes it at the end, the same way <see cref="HoldemEngine"/>
/// does, so the manager holds no connection between calls.
/// </summary>
internal sealed class HoldemSchemaManager(IDbContextFactory<HoldemDbContext> contextFactory) : IHoldemSchemaManager
{
    /// <summary>
    /// A relational database with no migrations is refused rather than created with
    /// <c>EnsureCreated</c>: a database created that way has no migration history, so it could never
    /// be migrated later.
    /// </summary>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var providerName = RequireSupportedProvider(context);
        var database = context.Database;

        if (!database.IsRelational())
        {
            await database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        if (!database.GetMigrations().Any())
        {
            throw new HoldemSchemaException(HoldemSchemaFailure.MigrationsNotFound, HoldemProviders.SetupHint(providerName), status: null);
        }

        await database.MigrateAsync(cancellationToken);
    }

    public async Task<HoldemSchemaStatus> GetSchemaStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var providerName = RequireSupportedProvider(context);
        var database = context.Database;

        if (!database.IsRelational())
        {
            return Status(HoldemSchemaState.NotVersioned, providerName, EquatableList<string>.Empty, databaseSchemaVersion: null, databaseEngineVersion: null);
        }

        var migrations = database.GetMigrations();
        var appliedMigrations = await database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = new EquatableList<string>(await database.GetPendingMigrationsAsync(cancellationToken));

        int? databaseSchemaVersion = null;
        string? databaseEngineVersion = null;

        var hasMigrations = migrations.Any();
        var hasMigrationsApplied = appliedMigrations.Any();

        if (hasMigrations && !hasMigrationsApplied)
        {
            var state = HoldemSchemaState.MigrationRequired;
            return Status(state, providerName, pending, databaseSchemaVersion: null, databaseEngineVersion: null);
        }

        var schemaRow = await context.Set<OheSchemaRow>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == 1, cancellationToken);
        if (schemaRow != null)
        {
            databaseSchemaVersion = schemaRow.SchemaVersion;
            databaseEngineVersion = schemaRow.EngineVersion;
        }

        var hasPendingMigrations = pending.Count > 0;
        var compareState = HoldemSchema.Compare(databaseSchemaVersion, hasPendingMigrations);
        return Status(compareState, providerName, pending, databaseSchemaVersion, databaseEngineVersion);
    }

    private static string RequireSupportedProvider(HoldemDbContext context)
    {
        var providerName = context.Database.ProviderName;
        if (!HoldemProviders.IsSupported(providerName))
        {
            throw new HoldemSchemaException(
                HoldemSchemaFailure.UnsupportedProvider,
                $"The EF Core provider '{providerName ?? "(none)"}' is not supported. The engine supports {string.Join(", ", HoldemProviders.All)}.",
                status: null);
        }

        return providerName!;
    }

    private static HoldemSchemaStatus Status(HoldemSchemaState state, string providerName, EquatableList<string> pending, int? databaseSchemaVersion, string? databaseEngineVersion)
        => new(state, providerName, HoldemSchema.Version, databaseSchemaVersion, databaseEngineVersion, pending);
}
