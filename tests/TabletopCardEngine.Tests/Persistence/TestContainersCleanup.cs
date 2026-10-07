namespace TabletopCardEngine.Tests.Persistence;

/// <summary>
/// Stops the PostgreSQL and SQL Server containers <see cref="TestDatabase"/> started, once the whole run
/// is over: they are shared by every test, so no single test can own them.
/// </summary>
[TestClass]
public static class TestContainersCleanup
{
    [AssemblyCleanup]
    public static async Task DisposeContainersAsync() => await TestDatabase.DisposeContainersAsync();
}
