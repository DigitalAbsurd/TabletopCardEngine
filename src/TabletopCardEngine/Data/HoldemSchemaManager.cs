using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Mappers;

namespace TabletopCardEngine.Data;

/// <summary>
/// Creates one context per call and disposes it at the end, the same way <see cref="HoldemTable"/>
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

    /// <summary>
    /// Ensures the database schema is compatible with this engine instance on its first call.
    /// <para>
    /// Checks once per engine instance and throws <see cref="HoldemSchemaException"/> on a mismatch:
    /// if the database is newer, if migrations are pending, or if the database is not versioned.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <exception cref="HoldemSchemaException">When the database version does not match the engine.</exception>
    internal async Task EnsureCompatibleAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var providerName = RequireSupportedProvider(context);
        var database = context.Database;

        if (!database.IsRelational())
        {
            return;
        }

        var migrations = database.GetMigrations();
        var appliedMigrations = await database.GetAppliedMigrationsAsync(cancellationToken);

        if (migrations.Any() && !appliedMigrations.Any())
        {
            throw new HoldemSchemaException(
                HoldemSchemaFailure.MigrationRequired,
                "The database schema is older than this engine. Call IHoldemSchemaManager.MigrateAsync() first.",
                status: null);
        }

        var schemaRow = await context.Set<TceSchemaRow>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == 1, cancellationToken);

        if (schemaRow is null)
        {
            throw new HoldemSchemaException(
                HoldemSchemaFailure.MigrationRequired,
                "The database schema is not versioned. Call IHoldemSchemaManager.MigrateAsync() first.",
                status: null);
        }

        var databaseSchemaVersion = schemaRow.SchemaVersion;
        var compareState = HoldemSchema.Compare(databaseSchemaVersion, hasPendingMigrations: false);

        switch (compareState)
        {
            case HoldemSchemaState.NewerThanEngine:
                var status = new HoldemSchemaStatus(
                    compareState,
                    providerName,
                    HoldemSchema.Version,
                    databaseSchemaVersion,
                    schemaRow.EngineVersion,
                    EquatableList<string>.Empty);
                throw new HoldemSchemaException(
                    HoldemSchemaFailure.DatabaseIsNewer,
                    $"The database schema (version {databaseSchemaVersion}, engine {schemaRow.EngineVersion}) is newer than this engine (version {HoldemSchema.Version}). The engine must be upgraded.",
                    status);
            case HoldemSchemaState.MigrationRequired:
                throw new HoldemSchemaException(
                    HoldemSchemaFailure.MigrationRequired,
                    "The database schema is older than this engine. Call IHoldemSchemaManager.MigrateAsync() first.",
                    status: null);
        }
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

        var schemaRow = await context.Set<TceSchemaRow>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == 1, cancellationToken);
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
