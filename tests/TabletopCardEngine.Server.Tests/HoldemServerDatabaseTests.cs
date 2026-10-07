using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace TabletopCardEngine.Server.Tests;

[TestClass]
public sealed class HoldemServerDatabaseTests
{
    [TestMethod]
    [DataRow("Sqlite", "Microsoft.EntityFrameworkCore.Sqlite")]
    [DataRow("postgresql", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [DataRow("SQLSERVER", "Microsoft.EntityFrameworkCore.SqlServer")]
    public void Configure_KnownProvider_SelectsIt(string providerName, string expectedAssemblyName)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HoldemServer:Provider", providerName },
                { "ConnectionStrings:Holdem", providerName == "postgresql" ? "Host=x;Database=y" : "Server=x;Database=y" },
            })
            .Build();

        var optionsAction = TabletopCardEngine.Server.HoldemServerDatabase.Configure(config);
        var options = new DbContextOptionsBuilder();
        optionsAction(options);

        var extension = options.Options.Extensions.OfType<RelationalOptionsExtension>().Single();
        extension.GetType().Assembly.GetName().Name.Should().Be(expectedAssemblyName);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("1")]
    [DataRow("MySql")]
    public void Configure_BadProvider_ThrowsNamingTheVariable(string? providerValue)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HoldemServer:Provider", providerValue },
                { "ConnectionStrings:Holdem", "Data Source=:memory:" },
            })
            .Build();

        var action = () => TabletopCardEngine.Server.HoldemServerDatabase.Configure(config);
        var exception = action.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("HoldemServer__Provider");
    }

    [TestMethod]
    public void Configure_MissingConnectionString_ThrowsNamingTheVariable()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HoldemServer:Provider", "Sqlite" },
            })
            .Build();

        var action = () => TabletopCardEngine.Server.HoldemServerDatabase.Configure(config);
        var exception = action.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("ConnectionStrings__Holdem");
    }
}
