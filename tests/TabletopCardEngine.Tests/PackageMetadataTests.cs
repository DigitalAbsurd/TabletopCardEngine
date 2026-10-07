using FluentAssertions;
using System.Text.Json;
using System.Xml.Linq;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Guards the nuspec of the seven packages. Nothing in the build fails on a missing description or
/// license: the SDK quietly falls back to "Package Description" and the assembly name as the author,
/// which is what v1.0.0-preview.2 shipped with. Reading the project files is enough here; the
/// release workflow packs the real thing.
/// </summary>
[TestClass]
public sealed class PackageMetadataTests
{
    private static readonly string[] _sharedProperties =
    [
        "Authors",
        "Copyright",
        "PackageLicenseExpression",
        "PackageProjectUrl",
        "RepositoryUrl",
        "PackageTags",
    ];

    [TestMethod]
    public void PackageMetadata_EveryPackage_HasItsOwnDescription()
    {
        var descriptions = ReadPackagesFilter()
            .ToDictionary(project => project, project => Property(Path.Combine(RepositoryRoot.FullPath, project), "Description"));

        descriptions.Values.Should().NotContain(description => string.IsNullOrWhiteSpace(description),
            "every packable project sets a Description, or the nuspec says \"Package Description\"");
        descriptions.Values.Should().OnlyHaveUniqueItems();
    }

    [TestMethod]
    public void PackageMetadata_SharedProperties_AreSetForEveryProject()
    {
        var props = Path.Combine(RepositoryRoot.FullPath, "Directory.Build.props");

        foreach (var name in _sharedProperties)
        {
            Property(props, name).Should().NotBeNullOrWhiteSpace($"Directory.Build.props sets {name}");
        }

        Property(props, "PackageLicenseExpression").Should().Be("Apache-2.0", "ADR-0001 licenses the project under Apache-2.0");
        Property(props, "PublishRepositoryUrl").Should().Be("true", "the nuspec's repository element needs its url");
    }

    private static string? Property(string projectPath, string name)
    {
        return XDocument.Load(projectPath)
            .Descendants(name)
            .Select(element => element.Value.Trim())
            .FirstOrDefault();
    }

    private static List<string> ReadPackagesFilter()
    {
        var filterPath = Path.Combine(RepositoryRoot.FullPath, "TabletopCardEngine.Packages.slnf");
        using var doc = JsonDocument.Parse(File.ReadAllText(filterPath));

        // The filter writes Windows separators, which Path does not split on outside Windows.
        return doc.RootElement.GetProperty("solution").GetProperty("projects")
            .EnumerateArray()
            .Select(elem => elem.GetString()!.Replace('\\', '/'))
            .ToList();
    }
}
