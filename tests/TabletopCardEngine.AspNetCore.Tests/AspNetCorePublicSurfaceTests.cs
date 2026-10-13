using FluentAssertions;

namespace TabletopCardEngine.AspNetCore.Tests;

/// <summary>
/// Verifies that the public API surface of the ASP.NET Core package complies with the design: the
/// retired engine-named API (ADR-0008) is kept out, and all public types are in the
/// <c>TabletopCardEngine.AspNetCore</c> namespace.
/// </summary>
[TestClass]
public sealed class AspNetCorePublicSurfaceTests
{
    [TestMethod]
    public void PublicTypes_AreInTheAspNetCoreNamespace()
    {
        var offenders = typeof(HoldemHostingBuilderExtensions).Assembly.GetExportedTypes()
            .Where(type => type.Namespace != "TabletopCardEngine.AspNetCore")
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty("every exported type of the ASP.NET Core package must be in the TabletopCardEngine.AspNetCore namespace");
    }

    [TestMethod]
    public void PublicTypes_DoNotNameTheEngineAfterAGame()
    {
        var retiredEngineName = "Holdem" + "Engine";
        var offenders = typeof(HoldemHostingBuilderExtensions).Assembly.GetExportedTypes()
            .Where(type => type.Name.Contains(retiredEngineName, StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty("no exported type name in the ASP.NET Core package must contain the retired engine-named API (ADR-0008)");
    }
}
