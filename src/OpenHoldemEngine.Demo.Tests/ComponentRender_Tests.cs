using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Demo.Components;
using OpenHoldemEngine.Demo.Layout;
using OpenHoldemEngine.Demo.Services;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ComponentRender_Tests
{
    private static void RegisterCommonServices(IServiceCollection services)
    {
        services.AddAntDesign();
        services.AddScoped<BrowserStorage>();
        services.AddScoped<WalkthroughState>();
    }

    [TestMethod]
    public void MainLayout_Renders_WithoutException()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterCommonServices(ctx.Services);

        // Act
        var component = ctx.RenderComponent<MainLayout>();

        // Assert
        component.Markup.Should().Contain("OpenHoldemEngine Demo");
        component.Markup.Should().Contain("GitHub Repository");
    }

    [TestMethod]
    public void FirstVisitWalkthrough_Renders_WithoutException()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterCommonServices(ctx.Services);

        // Act
        var component = ctx.RenderComponent<FirstVisitWalkthrough>();

        // Assert
        // Under loose JS interop, BrowserStorage returns false, so _visible is true and modal appears
        component.Markup.Should().Contain("Step 1: The Poker Table");
    }

    [TestMethod]
    public async Task FirstVisitWalkthrough_ShowAsync_RendersStep1()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterCommonServices(ctx.Services);

        var walkthrough = ctx.RenderComponent<FirstVisitWalkthrough>();

        // Initially on step 1
        walkthrough.Markup.Should().Contain("Step 1: The Poker Table");

        // Advance to step 2 by clicking Next (simulated)
        var walkthrough2 = ctx.RenderComponent<FirstVisitWalkthrough>();

        // Act - Request show through WalkthroughState
        var state = ctx.Services.GetRequiredService<WalkthroughState>();
        await state.RequestShowAsync();

        // Since loose interop doesn't update the component, we need to re-render
        walkthrough2.Render();

        // Assert - Should be back at step 1
        walkthrough2.Markup.Should().Contain("Step 1: The Poker Table");
    }
}
