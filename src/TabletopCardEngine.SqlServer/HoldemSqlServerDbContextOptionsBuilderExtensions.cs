using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data.Common;
using TabletopCardEngine.Data;

namespace TabletopCardEngine.SqlServer;

/// <summary>
/// Configures the engine's database as SQL Server with the engine's own migrations in the <c>tce</c> schema.
/// The plain <c>UseSqlServer</c> call is not enough: EF would look for migrations in the core assembly, which has
/// none, and <see cref="IHoldemSchemaManager.MigrateAsync"/> would fail with
/// <see cref="HoldemSchemaFailure.MigrationsNotFound"/>.
/// </summary>
public static class HoldemSqlServerDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Uses the SQL Server database at <paramref name="connectionString"/>, with the migrations of this
    /// package and the engine's own history table, <c>tce.__TceMigrationsHistory</c>.
    /// </summary>
    /// <param name="builder">The options passed to <c>AddHoldemEngine</c>.</param>
    /// <param name="connectionString">A SQL Server connection string, e.g. <c>Server=.;Database=holdem;Trusted_Connection=True;TrustServerCertificate=True</c>.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static DbContextOptionsBuilder UseHoldemSqlServer(this DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connectionString);

        builder.UseSqlServer(connectionString, Configure);
        return builder;
    }

    /// <summary>
    /// Uses an already created SQL Server connection, with the migrations of this package and the
    /// engine's own history table in the <c>tce</c> schema.
    /// </summary>
    /// <param name="builder">The options passed to <c>AddHoldemEngine</c>.</param>
    /// <param name="connection">A SQL Server connection the caller owns.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static DbContextOptionsBuilder UseHoldemSqlServer(this DbContextOptionsBuilder builder, DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connection);

        builder.UseSqlServer(connection, Configure);
        return builder;
    }

    private static void Configure(SqlServerDbContextOptionsBuilder sqlServer)
        => sqlServer
            .MigrationsAssembly(typeof(HoldemSqlServerDbContextOptionsBuilderExtensions).Assembly.GetName().Name)
            .MigrationsHistoryTable(HoldemSchema.MigrationsHistoryTable, HoldemSchema.DatabaseSchema);
}
