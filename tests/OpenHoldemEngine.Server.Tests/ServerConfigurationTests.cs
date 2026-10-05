using FluentAssertions;
using System.Text.Json;

namespace OpenHoldemEngine.Server.Tests;

/// <summary>Guards the configuration files that ship inside every image built from the server.</summary>
[TestClass]
public sealed class ServerConfigurationTests
{
    // A connection string here is baked into every image, and one that does not suit the
    // configured provider fails at start-up with an error that names neither the file nor the key.
    [TestMethod]
    public void Server_AppSettings_CarryNoConnectionString()
    {
        var path = Path.Combine(RepositoryRoot.FullPath, "src", "OpenHoldemEngine.Server", "appsettings.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        document.RootElement.TryGetProperty("ConnectionStrings", out _).Should().BeFalse("{0} must not carry a connection string", path);
    }
}
