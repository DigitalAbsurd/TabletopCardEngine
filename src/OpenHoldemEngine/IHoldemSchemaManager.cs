namespace OpenHoldemEngine;

/// <summary>
/// The public way to create and inspect the engine's database schema, so that a host never has to
/// reach the engine's internal database context to do it. <c>AddHoldemEngine</c> registers one per
/// container, over the same database configuration as the engine itself.
/// <para>
/// Exactly four providers are supported, recognised by their EF Core provider name: SQLite,
/// PostgreSQL (Npgsql), SQL Server and InMemory. Any other is rejected with
/// <see cref="HoldemSchemaFailure.UnsupportedProvider"/>.
/// </para>
/// </summary>
public interface IHoldemSchemaManager
{
    /// <summary>
    /// Brings the schema up to date. On a relational database this applies the engine's migrations,
    /// which come from the provider's <c>OpenHoldemEngine.*</c> package; on InMemory it creates the
    /// model, since there is nothing to migrate.
    /// </summary>
    /// <param name="cancellationToken">Cancels the migration.</param>
    /// <returns>A task that completes once the schema is up to date.</returns>
    /// <exception cref="HoldemSchemaException">
    /// The provider is not supported (<see cref="HoldemSchemaFailure.UnsupportedProvider"/>), or it is
    /// relational but configured without the package that carries the migrations
    /// (<see cref="HoldemSchemaFailure.MigrationsNotFound"/>). A failure to reach the database is not
    /// wrapped: it surfaces as the provider reports it.
    /// </exception>
    Task MigrateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports how the database's schema compares with the one this engine expects, without changing
    /// anything. For a supported provider it does not throw because the schema is behind; that is
    /// what <see cref="HoldemSchemaState.MigrationRequired"/> is for.
    /// </summary>
    /// <param name="cancellationToken">Cancels the inspection.</param>
    /// <returns>The schema's current status.</returns>
    /// <exception cref="HoldemSchemaException">The provider is not supported (<see cref="HoldemSchemaFailure.UnsupportedProvider"/>).</exception>
    Task<HoldemSchemaStatus> GetSchemaStatusAsync(CancellationToken cancellationToken = default);
}
