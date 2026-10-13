using FluentAssertions;
using System.Security.Cryptography;
using System.Text.Json;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Pins the v1 public API baselines. Package validation compares each package with its committed
/// baseline, but a break plus a re-pack of that baseline in one pull request would pass every gate;
/// the baselines are <c>binary</c> in <c>.gitattributes</c> and a re-pack never reproduces the same
/// bytes, so a pinned hash turns that into a red test. Additions need no re-pack and no new hash.
/// </summary>
[TestClass]
public sealed class ApiBaselineFreezeTests
{
    private static readonly IReadOnlyDictionary<string, string> _frozenHashes = new Dictionary<string, string>
    {
        ["TabletopCardEngine.1.0.0-baseline.nupkg"] = "a9309aeffa023ec74ec56434aaae6d725d7332dcc26f75ddc0927b577289a507",
        ["TabletopCardEngine.AspNetCore.1.0.0-baseline.nupkg"] = "c509510336921f54647845e3074f8d3f6d86584bab77cdc20f079b70d8373029",
        ["TabletopCardEngine.InMemory.1.0.0-baseline.nupkg"] = "cb4b8cc667c10637eee05a28c137136ff1292913f046de7cae626623d232bc90",
        ["TabletopCardEngine.OpenTelemetry.1.0.0-baseline.nupkg"] = "72964a142b93d85da0c588f7ef283c722615fbf995bafc76a4753e390cbfe216",
        ["TabletopCardEngine.PostgreSql.1.0.0-baseline.nupkg"] = "4aebc5eb9aed068a0069ec4e53c02acd1b253ed4943b08a8beed411227454a55",
        ["TabletopCardEngine.SqlServer.1.0.0-baseline.nupkg"] = "54bee4b3f7db72166d0592c0a131c8da606b378c1f5d96fa4791d6b1c9b576b4",
        ["TabletopCardEngine.Sqlite.1.0.0-baseline.nupkg"] = "0d509928f8b0ed8db704b85daea6ff7b3a8c936eaa0ded1cb18d080f3edc4d5a",
    };

    private static string BaselineDirectory => Path.Combine(RepositoryRoot.FullPath, "eng", "api-baseline");

    [TestMethod]
    public void ApiBaseline_FileSet_IsTheSevenPackages()
    {
        // The filter writes Windows separators, which Path does not split on outside Windows.
        var expected = ReadPackagesFilter()
            .Select(project => $"{Path.GetFileNameWithoutExtension(project.Replace('\\', '/'))}.1.0.0-baseline.nupkg")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var actual = Directory.GetFiles(BaselineDirectory)
            .Select(path => Path.GetFileName(path))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        expected.Should().HaveCount(7);
        actual.Should().Equal(expected);
    }

    [TestMethod]
    public void ApiBaseline_Hashes_AreFrozen()
    {
        var actual = _frozenHashes.Keys
            .ToDictionary(name => name, name => Hash(Path.Combine(BaselineDirectory, name)));

        actual.Should().Equal(_frozenHashes,
            "The v1 public API is frozen (docs/public-api.md, 'The baseline is frozen'). " +
            "A re-packed baseline means a breaking change; that needs a maintainer decision and a CHANGELOG entry, not a test update.");
    }

    private static string Hash(string path)
    {
        return File.Exists(path)
            ? Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)))
            : "<missing>";
    }

    private static List<string> ReadPackagesFilter()
    {
        var filterPath = Path.Combine(RepositoryRoot.FullPath, "TabletopCardEngine.Packages.slnf");
        using var doc = JsonDocument.Parse(File.ReadAllText(filterPath));
        return doc.RootElement.GetProperty("solution").GetProperty("projects")
            .EnumerateArray()
            .Select(elem => elem.GetString()!)
            .ToList();
    }
}
