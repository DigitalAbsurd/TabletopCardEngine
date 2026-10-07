using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System.Data.Common;
using TabletopCardEngine.Data;

namespace TabletopCardEngine.PostgreSql;

/// <summary>
/// Configures the engine's database as PostgreSQL with the engine's own migrations in the <c>tce</c> schema.
/// The plain <c>UseNpgsql</c> call is not enough: EF would look for migrations in the core assembly, which has
/// none, and <see cref="IHoldemSchemaManager.MigrateAsync"/> would fail with
/// <see cref="HoldemSchemaFailure.MigrationsNotFound"/>.
/// </summary>
public static class HoldemPostgreSqlDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Uses the PostgreSQL database at <paramref name="connectionString"/>, with the migrations of this
    /// package and the engine's own history table, <c>tce.__TceMigrationsHistory</c>.
    /// </summary>
    /// <param name="builder">The options passed to <c>AddHoldemEngine</c>.</param>
    /// <param name="connectionString">A PostgreSQL connection string, e.g. <c>Host=localhost;Database=holdem</c>.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static DbContextOptionsBuilder UseHoldemPostgreSql(this DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connectionString);

        builder.UseNpgsql(connectionString, Configure);
        return builder;
    }

    /// <summary>
    /// Uses an already created PostgreSQL data source, with the migrations of this package and the
    /// engine's own history table in the <c>tce</c> schema. This is the recommended form for Npgsql,
    /// allowing for connection pooling and advanced configuration.
    /// </summary>
    /// <param name="builder">The options passed to <c>AddHoldemEngine</c>.</param>
    /// <param name="dataSource">A PostgreSQL data source the caller owns.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static DbContextOptionsBuilder UseHoldemPostgreSql(this DbContextOptionsBuilder builder, DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(dataSource);

        builder.UseNpgsql(dataSource, Configure);
        return builder;
    }

    private static void Configure(NpgsqlDbContextOptionsBuilder npgsql)
        => npgsql
            .MigrationsAssembly(typeof(HoldemPostgreSqlDbContextOptionsBuilderExtensions).Assembly.GetName().Name)
            .MigrationsHistoryTable(HoldemSchema.MigrationsHistoryTable, HoldemSchema.DatabaseSchema);
}
