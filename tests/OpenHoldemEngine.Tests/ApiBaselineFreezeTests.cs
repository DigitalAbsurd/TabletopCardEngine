using FluentAssertions;
using System.Security.Cryptography;
using System.Text.Json;

namespace OpenHoldemEngine.Tests;

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
        ["OpenHoldemEngine.1.0.0-baseline.nupkg"] = "6b21e718be0e3456c4f090345c98bd95fb02661485824d7848efcebd7d7e171c",
        ["OpenHoldemEngine.AspNetCore.1.0.0-baseline.nupkg"] = "d3e1edca998fa3e03d9edd4d609563c8e35ecebb5694c02d2b716d6722b3efa0",
        ["OpenHoldemEngine.InMemory.1.0.0-baseline.nupkg"] = "ebf8ba05824658592dc4747199487623cfbd9f1d5b7e933404376d5efbe72354",
        ["OpenHoldemEngine.OpenTelemetry.1.0.0-baseline.nupkg"] = "1d8675195f30efaa6f26447a43db9cd2523ff65f1f90863cb321c507d40eb659",
        ["OpenHoldemEngine.PostgreSql.1.0.0-baseline.nupkg"] = "2e48c0d5465597a9f4d689609778c89cf3595ed0f5fe06a11dd5ec54d2948376",
        ["OpenHoldemEngine.SqlServer.1.0.0-baseline.nupkg"] = "d71d9ef6643c20840a32a93cbc4c14312a045ad5a08d4c28cdc898765edbb284",
        ["OpenHoldemEngine.Sqlite.1.0.0-baseline.nupkg"] = "fc7bb7acf6080e9441a19f99f1e4d09495bbd92648f9cac434e236d746fd362e",
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
        var filterPath = Path.Combine(RepositoryRoot.FullPath, "OpenHoldemEngine.Packages.slnf");
        using var doc = JsonDocument.Parse(File.ReadAllText(filterPath));
        return doc.RootElement.GetProperty("solution").GetProperty("projects")
            .EnumerateArray()
            .Select(elem => elem.GetString()!)
            .ToList();
    }
}
