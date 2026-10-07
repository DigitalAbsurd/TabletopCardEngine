using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TabletopCardEngine.Dto;
using TabletopCardEngine.InMemory;
using TabletopCardEngine.Options;

namespace TabletopCardEngine.AspNetCore.Tests;

[TestClass]
public sealed class ConfigurationTests
{
    private static IHost BuildHost(Dictionary<string, string?> settings, Action<HoldemEngineOptionsBuilder>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(settings.ToDictionary(p => "HoldemEngine:" + p.Key, p => p.Value));
        builder.Services.AddHoldemEngine(
            builder.Configuration.GetSection("HoldemEngine"),
            db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")),
            configure);
        return builder.Build();
    }

    private static async Task StartAsync(IHost host)
    {
        await host.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        await host.StartAsync();
    }

    private static Task<HoldemResult<GameData>> CreateGameAsync(IHost host, int maxPlayers)
    {
        return host.Services.GetRequiredService<HoldemEngine>().ApplyHoldemActionAsync(
            new HoldemActionCreateGame(Guid.NewGuid(), new GameOptionData { MaxPlayersCount = maxPlayers }),
            CancellationToken.None);
    }

    [TestMethod]
    public async Task Configuration_EmptySection_AllowsSixSeats()
    {
        using var host = BuildHost([]);
        await StartAsync(host);
        var result = await CreateGameAsync(host, 6);
        result.IsSuccess.Should().BeTrue();
        await host.StopAsync();
    }

    [TestMethod]
    public async Task Configuration_GamePlayersMaxCount_IsEnforced()
    {
        using var host = BuildHost(new() { ["GamePlayersMaxCount"] = "4" });
        await StartAsync(host);
        var tooMany = await CreateGameAsync(host, 6);
        tooMany.IsSuccess.Should().BeFalse();
        tooMany.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.InvalidGameOptions);

        var fits = await CreateGameAsync(host, 4);
        fits.IsSuccess.Should().BeTrue();
        await host.StopAsync();
    }

    [TestMethod]
    public async Task Configuration_OutOfRange_FailsOnStart()
    {
        using var host = BuildHost(new() { ["GamePlayersMinCount"] = "1" });
        var act = () => host.StartAsync();
        (await act.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain("GamePlayersMinCount");
    }

    [TestMethod]
    public async Task Configuration_MinAboveMax_FailsOnStart()
    {
        using var host = BuildHost(new() { ["GamePlayersMinCount"] = "5", ["GamePlayersMaxCount"] = "3" });
        var act = () => host.StartAsync();
        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [TestMethod]
    public async Task Configuration_OverridesWithSettingsFromCode()
    {
        using var host = BuildHost(
            new() { ["GamePlayersMaxCount"] = "5" },
            b => b.WithSettings(s => s.GamePlayersMaxCount = 3));
        await StartAsync(host);
        var result = await CreateGameAsync(host, 5);
        result.IsSuccess.Should().BeTrue();
        await host.StopAsync();
    }

    [TestMethod]
    public async Task Configuration_EnableSensitiveCardLogging_IsBound()
    {
        using var host = BuildHost(new() { ["EnableSensitiveCardLogging"] = "true" });
        await StartAsync(host);
        host.Services.GetRequiredService<HoldemEngineOptions>().Settings.EnableSensitiveCardLogging.Should().BeTrue();
        await host.StopAsync();
    }
}
