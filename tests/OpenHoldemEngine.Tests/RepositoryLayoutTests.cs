using FluentAssertions;
using System.Text.Json;
using System.Xml.Linq;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// Keeps the solution filter <c>OpenHoldemEngine.Packages.slnf</c> in step with the projects that
/// should be packed. Every project under <c>src/</c> without <c>&lt;IsPackable&gt;false&lt;/IsPackable&gt;</c>
/// must be listed in the filter's <c>solution.projects</c> array, and conversely. The filter is what gates
/// the public API baseline for every package, so a provider package added later cannot be left out.
/// </summary>
[TestClass]
public sealed class RepositoryLayoutTests
{
    [TestMethod]
    public void PackagesFilter_ListsEveryPackableProject()
    {
        var packableProjects = EnumeratePackableProjects().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filterProjects = ReadPackagesFilter().ToHashSet(StringComparer.OrdinalIgnoreCase);

        packableProjects.Should().BeEquivalentTo(filterProjects,
            "the solution filter must list every packable project, and only packable projects");
    }

    private static IEnumerable<string> EnumeratePackableProjects()
    {
        var srcDir = Path.Combine(RepositoryRoot.FullPath, "src");
        var projects = Directory
            .EnumerateFiles(srcDir, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
        projects.Should().NotBeEmpty();

        foreach (var project in projects)
        {
            var doc = XDocument.Load(project);
            var isPackable = doc.Root!.Elements("PropertyGroup")
                .Elements("IsPackable")
                .FirstOrDefault()?.Value;

            if (isPackable != "false")
            {
                var relativePath = Path.GetRelativePath(RepositoryRoot.FullPath, project);
                // Normalize to backslash paths as they appear in the .sln file
                yield return relativePath.Replace(Path.DirectorySeparatorChar, '\\');
            }
        }
    }

    private static List<string> ReadPackagesFilter()
    {
        var filterPath = Path.Combine(RepositoryRoot.FullPath, "OpenHoldemEngine.Packages.slnf");
        File.Exists(filterPath).Should().BeTrue("the solution filter must exist at {0}", filterPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(filterPath));
        // MSBuild only reads a filter whose path and projects sit under a top-level "solution" object.
        return doc.RootElement.GetProperty("solution").GetProperty("projects")
            .EnumerateArray()
            .Select(elem => elem.GetString()!.Replace('/', '\\'))
            .OrderBy(x => x)
            .ToList();
    }
}
