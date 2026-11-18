using AntDesign.JsInterop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace TabletopCardEngine.Demo.Tests;

/// <summary>
/// AntDesign's services for a bUnit test, with the DOM event listener replaced by one that does nothing.
/// </summary>
/// <remarks>
/// AntDesign keeps a scope's shared DOM event subscriptions in plain lists, which are not safe across
/// threads. Under bUnit no browser ever raises those events, yet an overlay's delayed show or hide still
/// adds and removes subscriptions on the renderer's dispatcher, while disposing the context walks the
/// same lists from the test's thread, and that is the intermittent "Collection was modified" thrown from
/// <c>DomEventListener.DisposeShared</c>. With the listener inert there is nothing left to race on.
/// AntDesign's own test kit replaces the listener the same way.
/// </remarks>
internal static class AntDesignTestServices
{
    public static IServiceCollection AddAntDesignForTests(this IServiceCollection services)
    {
        // Registered first, because AddAntDesign only adds its own DomEventService when there is none.
        services.AddScoped<DomEventService, InertDomEventService>();
        services.AddAntDesign();
        return services;
    }

    private sealed class InertDomEventService(IJSRuntime jsRuntime) : DomEventService(jsRuntime)
    {
        public override IDomEventListener CreateDomEventListerner() => Mock.Of<IDomEventListener>();
    }
}
