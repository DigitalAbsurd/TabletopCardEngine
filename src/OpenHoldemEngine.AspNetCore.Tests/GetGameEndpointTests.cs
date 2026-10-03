using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.InMemory;
using System.Net;
using System.Text.Json;

namespace OpenHoldemEngine.AspNetCore.Tests;

[TestClass]
public sealed class GetGameEndpointTests
{
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static IEnumerable<RouteEndpoint> RouteEndpoints(TestApiHost host)
        => ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(d => d.Endpoints).OfType<RouteEndpoint>();

    [TestMethod]
    public async Task GetGame_Existing_Returns200WithETagAndContract()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        var response = await host.Client.GetAsync($"/api/holdem/games/{gameId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.Should().Be("\"2\"");
        var body = await ReadJsonAsync(response);
        body.GetProperty("gameId").GetGuid().Should().Be(gameId);
        var handData = body.GetProperty("handData");
        handData.GetProperty("street").GetString().Should().Be("PreFlop");
        foreach (var seat in handData.GetProperty("seats").EnumerateArray())
        {
            foreach (var card in seat.GetProperty("hole").EnumerateArray())
            {
                card.ValueKind.Should().Be(JsonValueKind.String);
            }
        }
    }

    [TestMethod]
    public async Task GetGame_Unknown_Returns404Problem()
    {
        await using var host = await TestApiHost.StartAsync();

        var response = await host.Client.GetAsync($"/api/holdem/games/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("GameNotFound");
    }

    [TestMethod]
    [DataRow("\"2\"", HttpStatusCode.OK, null)]
    [DataRow("\"1\"", HttpStatusCode.PreconditionFailed, "Conflict")]
    [DataRow("*", HttpStatusCode.OK, null)]
    [DataRow("W/\"2\"", HttpStatusCode.BadRequest, null)]
    public async Task GetGame_IfMatch(string ifMatch, HttpStatusCode expectedStatus, string? expectedCode)
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/holdem/games/{gameId}");
        request.Headers.TryAddWithoutValidation("If-Match", ifMatch);

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(expectedStatus);
        if (expectedStatus != HttpStatusCode.OK)
        {
            var problem = await ReadJsonAsync(response);
            if (expectedCode is null)
            {
                problem.TryGetProperty("code", out _).Should().BeFalse();
            }
            else
            {
                problem.GetProperty("code").GetString().Should().Be(expectedCode);
            }
        }
    }

    [TestMethod]
    public async Task GetGame_NotAGuid_Returns404()
    {
        await using var host = await TestApiHost.StartAsync();

        var response = await host.Client.GetAsync("/api/holdem/games/abc");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task MapHoldemEngineApi_NotCalled_MapsNothing()
    {
        await using var host = await TestApiHost.StartAsync(map: _ => { });
        var gameId = await host.SeedHeadsUpGameAsync();

        var response = await host.Client.GetAsync($"/api/holdem/games/{gameId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        RouteEndpoints(host).Should().NotContain(e => e.RoutePattern.RawText!.Contains("games"));
    }

    [TestMethod]
    public async Task MapHoldemEngineApi_CustomPrefix()
    {
        await using var host = await TestApiHost.StartAsync(map: app => app.MapHoldemEngineApi("/poker"));
        var gameId = await host.SeedHeadsUpGameAsync();

        var response = await host.Client.GetAsync($"/poker/games/{gameId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task MapHoldemEngineApi_ReturnsTheGroup()
    {
        await using var host = await TestApiHost.StartAsync(
            map: app => app.MapHoldemEngineApi().RequireAuthorization(),
            configure: b => b.Services.AddAuthorization());

        var endpoint = RouteEndpoints(host).Single(e => e.RoutePattern.RawText!.Contains("games"));

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task MapHoldemEngineApi_WithoutThePackageRegistration_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHoldemEngine(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")));
        await using var app = builder.Build();

        var map = () => app.MapHoldemEngineApi();

        map.Should().Throw<InvalidOperationException>().WithMessage("*AddHoldemEngine(configuration*");
    }
}
