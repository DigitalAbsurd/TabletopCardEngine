namespace TabletopCardEngine.Server.Tests;

/// <summary>
/// The repository's root directory, found by walking up from the test binaries to
/// <c>TabletopCardEngine.sln</c>. Used by the tests that read files outside their own output, such
/// as the server's <c>appsettings.json</c>.
/// </summary>
internal static class RepositoryRoot
{
    private static readonly Lazy<string> _fullPath = new(Find);

    internal static string FullPath => _fullPath.Value;

    private static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TabletopCardEngine.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not find TabletopCardEngine.sln above {AppContext.BaseDirectory}.");
    }
}
