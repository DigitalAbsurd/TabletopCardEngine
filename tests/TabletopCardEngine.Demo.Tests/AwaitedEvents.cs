using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace TabletopCardEngine.Demo.Tests;

/// <summary>
/// bUnit's event helpers that return only once the renderer has taken the event.
/// </summary>
/// <remarks>
/// bUnit's synchronous <c>Click</c>, <c>Change</c>, <c>Input</c> and <c>TriggerEvent</c> hand the event to
/// the renderer's dispatcher and do not wait for it. While the dispatcher is free the handler runs
/// inline, so the next line sees its effect. While it is busy, which is often the case with AntDesign
/// because its overlays show and hide behind <c>Task.Delay</c>, the event waits in the queue and the next
/// line asserts before the handler has run: a callback reads as never raised, a menu as empty.
/// <para>
/// These queue an empty work item behind the event and wait for it. The dispatcher runs its queue in
/// order, so by then the handler has run up to its first real await, exactly as it does when the
/// dispatcher is free. Waiting for the handler's whole task instead does not work: a dropdown's show
/// then runs out its one-second wait for the overlay's first render, and the menu never opens.
/// </para>
/// </remarks>
internal static class AwaitedEvents
{
    public static void ClickAndWait(this IElement element)
    {
        var dispatcher = DispatcherOf(element);
        element.Click();
        Wait(dispatcher);
    }

    public static void ChangeAndWait<T>(this IElement element, T value)
    {
        var dispatcher = DispatcherOf(element);
        element.Change(value);
        Wait(dispatcher);
    }

    public static void InputAndWait<T>(this IElement element, T value)
    {
        var dispatcher = DispatcherOf(element);
        element.Input(value);
        Wait(dispatcher);
    }

    public static void TriggerEventAndWait(this IElement element, string eventName, EventArgs eventArgs)
    {
        var dispatcher = DispatcherOf(element);
        element.TriggerEvent(eventName, eventArgs);
        Wait(dispatcher);
    }

    // Read before the event: the element is found again on every access, and the event may remove it.
    private static Dispatcher DispatcherOf(IElement element)
        => (element.Owner?.Context.GetService<BunitContext>()
            ?? throw new InvalidOperationException("The element was not rendered by bUnit.")).Renderer.Dispatcher;

    private static void Wait(Dispatcher dispatcher)
        => dispatcher.InvokeAsync(() => { }).GetAwaiter().GetResult();
}
