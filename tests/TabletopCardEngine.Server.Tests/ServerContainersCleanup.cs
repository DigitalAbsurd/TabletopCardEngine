namespace TabletopCardEngine.Server.Tests;

/// <summary>
/// Stops the PostgreSQL and SQL Server containers <see cref="ServerDatabases"/> started, once the whole
/// run is over: they are shared by every test, so no single test can own them.
/// </summary>
[TestClass]
public static class ServerContainersCleanup
{
    [AssemblyCleanup]
    public static async Task DisposeContainersAsync() => await ServerDatabases.DisposeContainersAsync();
}
