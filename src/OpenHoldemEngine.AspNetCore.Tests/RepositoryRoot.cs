namespace OpenHoldemEngine.AspNetCore.Tests;

/// <summary>
/// The repository's root directory, found by walking up from the test binaries to
/// <c>OpenHoldemEngine.sln</c>. Shared by the tests that read files outside their own output, such
/// as the notices and the restored <c>project.assets.json</c> files.
/// </summary>
internal static class RepositoryRoot
{
    private static readonly Lazy<string> _fullPath = new(Find);

    internal static string FullPath => _fullPath.Value;

    private static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OpenHoldemEngine.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not find OpenHoldemEngine.sln above {AppContext.BaseDirectory}.");
    }
}
