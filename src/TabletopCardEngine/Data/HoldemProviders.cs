namespace TabletopCardEngine.Data;

/// <summary>
/// The EF Core providers the engine supports, by <c>Database.ProviderName</c>. They are recognised by
/// name rather than by a type from the provider's package, so that the core references no database
/// driver and a SQLite consumer never pulls in SqlClient or Npgsql.
/// </summary>
internal static class HoldemProviders
{
    internal const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";

    internal const string PostgreSql = "Npgsql.EntityFrameworkCore.PostgreSQL";

    internal const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";

    internal const string InMemory = "Microsoft.EntityFrameworkCore.InMemory";

    internal static IReadOnlyList<string> All { get; } = [Sqlite, PostgreSql, SqlServer, InMemory];

    /// <summary>Ordinal: EF reports the provider's exact assembly name, so a differently cased name is some other provider.</summary>
    internal static bool IsSupported(string? providerName)
        => providerName is not null && All.Contains(providerName, StringComparer.Ordinal);

    /// <summary>
    /// What a consumer who configured <paramref name="providerName"/> with the plain EF Core call has
    /// to do instead: the provider's engine package carries the migrations, and its <c>UseHoldem*</c>
    /// call is what points EF at them.
    /// </summary>
    internal static string SetupHint(string providerName) => providerName switch
    {
        Sqlite => Hint("TabletopCardEngine.Sqlite", "UseHoldemSqlite"),
        PostgreSql => Hint("TabletopCardEngine.PostgreSql", "UseHoldemPostgreSql"),
        SqlServer => Hint("TabletopCardEngine.SqlServer", "UseHoldemSqlServer"),
        InMemory => Hint("TabletopCardEngine.InMemory", "UseHoldemInMemory"),
        _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, "The provider is not supported."),
    };

    private static string Hint(string package, string call)
        => $"reference the {package} package and configure the engine with {call}(...)";
}
