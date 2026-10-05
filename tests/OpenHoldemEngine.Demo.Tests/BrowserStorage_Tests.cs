using FluentAssertions;
using Microsoft.JSInterop;
using Moq;
using OpenHoldemEngine.Demo.Services;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
public sealed class BrowserStorage_Tests
{
    [TestMethod]
    public async Task GetFlagAsync_WithStoredTrue_ReturnsTrue()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>();
        var mockModule = new Mock<IJSObjectReference>();

        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(mockModule.Object);

        mockModule
            .Setup(m => m.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(true);

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var result = await storage.GetFlagAsync("test-key");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetFlagAsync_WithAbsentKey_ReturnsFalse()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>();
        var mockModule = new Mock<IJSObjectReference>();

        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(mockModule.Object);

        mockModule
            .Setup(m => m.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(false);

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var result = await storage.GetFlagAsync("absent-key");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetFlagAsync_WhenJSThrows_ReturnsFalse()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>();
        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("Module not found"));

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var result = await storage.GetFlagAsync("test-key");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetFlagAsync_WhenInvokeAsyncThrows_ReturnsFalse()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>();
        var mockModule = new Mock<IJSObjectReference>();

        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(mockModule.Object);

        mockModule
            .Setup(m => m.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("localStorage unavailable"));

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var result = await storage.GetFlagAsync("test-key");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetFlagAsync_WhenInvokeAsyncThrowsInvalidOperationException_ReturnsFalse()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>();
        var mockModule = new Mock<IJSObjectReference>();

        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(mockModule.Object);

        mockModule
            .Setup(m => m.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new InvalidOperationException("JS interop not available"));

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var result = await storage.GetFlagAsync("test-key");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task SetFlagAsync_WithTrueValue_Succeeds()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>(MockBehavior.Loose);
        var mockModule = new Mock<IJSObjectReference>(MockBehavior.Loose);

        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(mockModule.Object);

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var act = () => storage.SetFlagAsync("test-key", true);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task SetFlagAsync_WithFalseValue_Succeeds()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>(MockBehavior.Loose);
        var mockModule = new Mock<IJSObjectReference>(MockBehavior.Loose);

        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(mockModule.Object);

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var act = () => storage.SetFlagAsync("test-key", false);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task SetFlagAsync_WhenJSThrows_DoesNotPropagateException()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>();
        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("Module not found"));

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        var act = () => storage.SetFlagAsync("test-key", true);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ModuleIsCached_AfterFirstImport()
    {
        // Arrange
        var mockJs = new Mock<IJSRuntime>();
        var mockModule = new Mock<IJSObjectReference>();
        var invocationCount = 0;

        mockJs
            .Setup(js => js.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback(() => invocationCount++)
            .ReturnsAsync(mockModule.Object);

        mockModule
            .Setup(m => m.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(false);

        var storage = new BrowserStorage(mockJs.Object);

        // Act
        await storage.GetFlagAsync("key1");
        await storage.GetFlagAsync("key2");

        // Assert - import should only be called once
        invocationCount.Should().Be(1);
    }
}
