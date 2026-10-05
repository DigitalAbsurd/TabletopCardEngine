using FluentAssertions;
using OpenHoldemEngine.Demo.Services;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
public sealed class WalkthroughState_Tests
{
    [TestMethod]
    public async Task RequestShowAsync_WithNoSubscriber_CompletesWithoutThrow()
    {
        // Arrange
        var state = new WalkthroughState();

        // Act
        var act = () => state.RequestShowAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task RequestShowAsync_WithSubscriber_InvokesShowRequested()
    {
        // Arrange
        var state = new WalkthroughState();
        var handlerInvoked = false;

        state.ShowRequested += async () =>
        {
            handlerInvoked = true;
            await Task.CompletedTask;
        };

        // Act
        await state.RequestShowAsync();

        // Assert
        handlerInvoked.Should().BeTrue();
    }

    [TestMethod]
    public async Task RequestShowAsync_WithMultipleSubscribers_InvokesAll()
    {
        // Arrange
        var state = new WalkthroughState();
        var handler1Invoked = false;
        var handler2Invoked = false;

        state.ShowRequested += async () =>
        {
            handler1Invoked = true;
            await Task.CompletedTask;
        };

        state.ShowRequested += async () =>
        {
            handler2Invoked = true;
            await Task.CompletedTask;
        };

        // Act
        await state.RequestShowAsync();

        // Assert
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
    }

    [TestMethod]
    public async Task RequestShowAsync_WithUnsubscribedHandler_DoesNotInvokeIt()
    {
        // Arrange
        var state = new WalkthroughState();
        var handlerInvoked = false;

        Func<Task> handler = async () =>
        {
            handlerInvoked = true;
            await Task.CompletedTask;
        };

        state.ShowRequested += handler;
        state.ShowRequested -= handler;

        // Act
        await state.RequestShowAsync();

        // Assert
        handlerInvoked.Should().BeFalse();
    }

    [TestMethod]
    public async Task RequestShowAsync_WhenHandlerThrows_PropagatesException()
    {
        // Arrange
        var state = new WalkthroughState();
        var expectedException = new InvalidOperationException("Test error");

        state.ShowRequested += async () =>
        {
            await Task.CompletedTask;
            throw expectedException;
        };

        // Act
        var act = () => state.RequestShowAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test error");
    }
}
