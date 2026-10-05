using FluentAssertions;
using System.Text.Json;

namespace OpenHoldemEngine.AspNetCore.Tests;

/// <summary>
/// The guard on <c>OpenHoldemEngine.AspNetCore</c>'s dependency rule (ADR-0004): the engine and the
/// shared framework, no NuGet package, and no OpenTelemetry anywhere in its graph.
/// </summary>
[TestClass]
public sealed class AspNetCorePackageTests
{
    [TestMethod]
    public void Package_DependsOnlyOnTheEngineAndTheSharedFramework()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "OpenHoldemEngine.AspNetCore", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the package must be restored before the tests run");
        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
        var framework = assets.RootElement.GetProperty("project").GetProperty("frameworks").GetProperty("net10.0");

        var direct = framework.TryGetProperty("dependencies", out var dependencies)
            ? dependencies.EnumerateObject()
                .Where(dependency => dependency.Name != "MinVer")
                .Select(dependency => dependency.Name).ToList()
            : [];
        direct.Should().BeEmpty("the package takes no NuGet package, not even a Microsoft.Extensions one: the shared framework carries those; MinVer is build-time only with PrivateAssets=\"all\" (fact 30)");

        // Microsoft.NETCore.App is the runtime every net10.0 project references implicitly.
        framework.GetProperty("frameworkReferences").EnumerateObject()
            .Select(reference => reference.Name)
            .Where(name => name != "Microsoft.NETCore.App")
            .Should().Equal("Microsoft.AspNetCore.App");

        assets.RootElement.GetProperty("libraries").EnumerateObject()
            .Select(library => library.Name)
            .Where(name => name.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty("the OpenTelemetry wiring lives in OpenHoldemEngine.OpenTelemetry, never in this package");
    }
}
