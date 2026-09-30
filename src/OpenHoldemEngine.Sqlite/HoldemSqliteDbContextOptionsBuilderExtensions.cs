using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenHoldemEngine.Data;
using System.Data.Common;

namespace OpenHoldemEngine.Sqlite;

/// <summary>
/// Configures the engine's database as SQLite with the engine's own migrations. The plain
/// <c>UseSqlite</c> call is not enough: EF would look for migrations in the core assembly, which has
/// none, and <see cref="IHoldemSchemaManager.MigrateAsync"/> would fail with
/// <see cref="HoldemSchemaFailure.MigrationsNotFound"/>.
/// </summary>
public static class HoldemSqliteDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Uses the SQLite database at <paramref name="connectionString"/>, with the migrations of this
    /// package and the engine's own history table, <c>__OheMigrationsHistory</c>.
    /// </summary>
    /// <param name="builder">The options passed to <c>AddHoldemEngine</c>.</param>
    /// <param name="connectionString">A SQLite connection string, e.g. <c>Data Source=holdem.db</c>.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static DbContextOptionsBuilder UseHoldemSqlite(this DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connectionString);

        builder.UseSqlite(connectionString, Configure);
        return builder;
    }

    /// <summary>
    /// Uses an already created SQLite connection, with the migrations of this package and the
    /// engine's own history table. This is the overload for an in-memory database, which lives only
    /// as long as its connection stays open, so the caller keeps it open and disposes of it.
    /// </summary>
    /// <param name="builder">The options passed to <c>AddHoldemEngine</c>.</param>
    /// <param name="connection">A SQLite connection the caller owns.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static DbContextOptionsBuilder UseHoldemSqlite(this DbContextOptionsBuilder builder, DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connection);

        builder.UseSqlite(connection, Configure);
        return builder;
    }

    private static void Configure(SqliteDbContextOptionsBuilder sqlite)
        => sqlite
            .MigrationsAssembly(typeof(HoldemSqliteDbContextOptionsBuilderExtensions).Assembly.GetName().Name)
            .MigrationsHistoryTable(HoldemSchema.MigrationsHistoryTable);
}
