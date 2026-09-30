using FluentAssertions;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// Keeps <c>THIRD-PARTY-NOTICES.md</c> true to what the projects under <c>src/</c> reference. Every
/// NuGet package a project references directly must have a row in the notices' package inventory
/// with the license its nuspec declares, and the inventory must not keep rows for packages no
/// project references. The inventory skips transitive packages, but their licenses are still
/// checked: every package in every <c>project.assets.json</c> must be on the permissive allowlist.
/// A new dependency, a version bump or a relicensed package therefore fails here until
/// <c>scripts/audit-licenses.ps1</c> is re-run and its tables are pasted into the notices.
/// </summary>
[TestClass]
public sealed partial class ThirdPartyNoticesTests
{
    private const string NoticesFileName = "THIRD-PARTY-NOTICES.md";

    // The same allowlist scripts/audit-licenses.ps1 enforces.
    private static readonly HashSet<string> _permissiveLicenses =
    [
        "MIT",
        "Apache-2.0",
        "BSD-2-Clause",
        "BSD-3-Clause",
        // The pre-SPDX Microsoft .NET Library License of the netstandard1.x-era packages. None of
        // their assemblies is published: the runtime pack's newer ones replace them (see the notices).
        "MS-.NET-Library",
    ];

    // Packages that predate nuspec license expressions and carry only a URL. Kept in step with
    // $legacyLicenseUrls in scripts/audit-licenses.ps1.
    private static readonly Dictionary<string, string> _legacyLicenseUrls = new(StringComparer.Ordinal)
    {
        ["http://go.microsoft.com/fwlink/?LinkId=329770"] = "MS-.NET-Library",
        ["https://github.com/dotnet/corefx/blob/master/LICENSE.TXT"] = "MIT",
        ["https://raw.githubusercontent.com/dotnet/corefx/master/LICENSE.TXT"] = "MIT",
        // OneOf 2.1.155; the linked licence.md is the MIT license, (c) 2016 Harry McIntyre.
        ["https://github.com/mcintyre321/OneOf/blob/master/licence.md"] = "MIT",
    };

    [TestMethod]
    public void ThirdPartyNotices_ListsEveryDirectPackageWithItsLicense()
    {
        var listed = ReadListedPackages();

        var problems = new List<string>();
        foreach (var (package, license) in ReadPackages().Direct)
        {
            if (!listed.TryGetValue(package, out var listedLicense))
            {
                problems.Add($"{package} ({license}) has no row");
            }
            else if (listedLicense != license)
            {
                problems.Add($"{package} is listed as {listedLicense}, but its nuspec says {license}");
            }
        }

        problems.Should().BeEmpty(
            "every directly referenced package must be in {0}; re-run scripts/audit-licenses.ps1", NoticesFileName);
    }

    [TestMethod]
    public void ThirdPartyNotices_ListsNoPackageThatIsNotReferenced()
    {
        var direct = ReadPackages().Direct;

        ReadListedPackages().Keys.Where(package => !direct.ContainsKey(package)).Should().BeEmpty(
            "{0} lists only packages a project references directly; re-run scripts/audit-licenses.ps1",
            NoticesFileName);
    }

    [TestMethod]
    public void ThirdPartyNotices_EveryResolvedPackageIsPermissivelyLicensed()
    {
        ReadPackages().Resolved
            .Where(pair => !_permissiveLicenses.Contains(pair.Value))
            .Select(pair => $"{pair.Key}: {pair.Value}")
            .Should().BeEmpty("a license outside the allowlist needs a legal review before it is added");
    }

    /// <summary>
    /// Rows of the tables under "## Package inventory", keyed <c>Id/Version</c> as the assets file
    /// keys them. The component table above that section has multi-word cells and is not read.
    /// </summary>
    private static Dictionary<string, string> ReadListedPackages()
    {
        var notices = File.ReadAllText(Path.Combine(RepositoryRoot.FullPath, NoticesFileName));
        var start = notices.IndexOf("\n## Package inventory", StringComparison.Ordinal);
        var end = notices.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        start.Should().BeGreaterThan(0, "{0} must have a '## Package inventory' section", NoticesFileName);
        var inventory = end < 0 ? notices[start..] : notices[start..end];

        var listed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match row in InventoryRow().Matches(inventory))
        {
            if (row.Groups["id"].Value is "Package" or "---")
            {
                continue;
            }

            var package = $"{row.Groups["id"].Value}/{row.Groups["version"].Value}";
            listed.TryAdd(package, row.Groups["license"].Value).Should().BeTrue("{0} is listed twice", package);
        }

        listed.Should().NotBeEmpty();
        return listed;
    }

    /// <summary>
    /// Every NuGet package restore resolved for any project under <c>src/</c> (<c>Resolved</c>), and
    /// the subset some project references itself (<c>Direct</c>), both keyed <c>Id/Version</c> with
    /// the license its nuspec declares. The packages the SDK adds on its own, such as
    /// <c>Microsoft.NET.ILLink.Tasks</c>, are <c>autoReferenced</c> in the assets file and are not
    /// direct.
    /// </summary>
    private static (Dictionary<string, string> Resolved, Dictionary<string, string> Direct) ReadPackages()
    {
        var projects = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot.FullPath, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
        projects.Should().NotBeEmpty();

        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var direct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in projects)
        {
            var assetsFile = Path.Combine(Path.GetDirectoryName(project)!, "obj", "project.assets.json");
            File.Exists(assetsFile).Should().BeTrue("{0} must be restored before the tests run", project);

            using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
            var packageFolders = assets.RootElement.GetProperty("packageFolders")
                .EnumerateObject().Select(folder => folder.Name).ToList();

            var keyById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var library in assets.RootElement.GetProperty("libraries").EnumerateObject())
            {
                if (library.Value.GetProperty("type").GetString() != "package")
                {
                    continue;
                }

                var id = library.Name[..library.Name.IndexOf('/', StringComparison.Ordinal)];
                keyById[id] = library.Name;
                if (!resolved.ContainsKey(library.Name))
                {
                    var path = library.Value.GetProperty("path").GetString()!;
                    resolved[library.Name] = ReadNuspecLicense(packageFolders, path, id);
                }
            }

            foreach (var framework in assets.RootElement.GetProperty("project").GetProperty("frameworks").EnumerateObject())
            {
                if (!framework.Value.TryGetProperty("dependencies", out var dependencies))
                {
                    continue;
                }

                foreach (var dependency in dependencies.EnumerateObject())
                {
                    var autoReferenced = dependency.Value.TryGetProperty("autoReferenced", out var flag) && flag.GetBoolean();
                    if (!autoReferenced && keyById.TryGetValue(dependency.Name, out var key))
                    {
                        direct[key] = resolved[key];
                    }
                }
            }
        }

        return (resolved, direct);
    }

    private static string ReadNuspecLicense(List<string> packageFolders, string path, string id)
    {
        var nuspec = packageFolders
            .Select(folder => Path.Combine(folder, path, $"{id.ToLowerInvariant()}.nuspec"))
            .FirstOrDefault(File.Exists);
        nuspec.Should().NotBeNull("the nuspec of {0} must be in a NuGet package folder", path);

        var metadata = XDocument.Load(nuspec!).Root!.Elements().Single(element => element.Name.LocalName == "metadata");
        var license = metadata.Elements().SingleOrDefault(element => element.Name.LocalName == "license");
        if (license is not null)
        {
            var type = (string?)license.Attribute("type");
            return type == "expression" ? license.Value : $"{type}:{license.Value}";
        }

        var url = metadata.Elements().SingleOrDefault(element => element.Name.LocalName == "licenseUrl")?.Value ?? "";
        return _legacyLicenseUrls.TryGetValue(url, out var known) ? known : $"url:{url}";
    }

    [GeneratedRegex(@"^\| (?<id>[^\s|]+) \| (?<version>[^\s|]+) \| (?<license>[^\s|]+) \|\r?$", RegexOptions.Multiline)]
    private static partial Regex InventoryRow();
}
