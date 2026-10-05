using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.AspNetCore.Tests;

/// <summary>Tests for OpenAPI metadata declarations on the REST endpoints.</summary>
[TestClass]
public sealed class OpenApiMetadataTests
{
    [TestMethod]
    public async Task ApiExplorer_ReportsSixEndpoints()
    {
        await using var host = await TestApiHost.StartAsync(configure: b => b.Services.AddEndpointsApiExplorer());
        var apiDescriptionProvider = host.App.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var descriptions = apiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(g => g.Items).ToList();

        var expectedEndpoints = new[]
        {
            ("GET", "api/holdem/games/{gameId:guid}"),
            ("POST", "api/holdem/games"),
            ("POST", "api/holdem/games/{gameId:guid}/start"),
            ("POST", "api/holdem/games/{gameId:guid}/hands"),
            ("POST", "api/holdem/games/{gameId:guid}/actions"),
            ("POST", "api/holdem/games/{gameId:guid}/finish"),
        };

        var actualEndpoints = descriptions
            .Select(d => (d.HttpMethod!, d.RelativePath!))
            .OrderBy(x => (x.Item1, x.Item2))
            .ToList();

        actualEndpoints.Should().HaveCount(6);
        foreach (var expected in expectedEndpoints.OrderBy(x => (x.Item1, x.Item2)))
        {
            actualEndpoints.Should().Contain(expected);
        }
    }

    [TestMethod]
    public async Task ApiExplorer_EveryEndpoint_DeclaresGameDataOnSuccess()
    {
        await using var host = await TestApiHost.StartAsync(configure: b => b.Services.AddEndpointsApiExplorer());
        var apiDescriptionProvider = host.App.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var descriptions = apiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(g => g.Items).ToList();

        foreach (var d in descriptions)
        {
            var successResponse = d.SupportedResponseTypes.FirstOrDefault(r => r.IsDefaultResponse == false);
            successResponse.Should().NotBeNull($"endpoint {d.HttpMethod} {d.RelativePath} should declare a success response");
            successResponse!.Type.Should().Be<GameData>();
            successResponse.ApiResponseFormats.Should().Contain(f => f.MediaType == "application/json");

            var expectedStatus = d.HttpMethod == "POST" && d.RelativePath == "api/holdem/games"
                ? StatusCodes.Status201Created
                : StatusCodes.Status200OK;
            successResponse.StatusCode.Should().Be(expectedStatus, $"endpoint {d.HttpMethod} {d.RelativePath} should return status {expectedStatus}");
        }
    }

    [TestMethod]
    public async Task ApiExplorer_EveryEndpoint_DeclaresItsProblems()
    {
        await using var host = await TestApiHost.StartAsync(configure: b => b.Services.AddEndpointsApiExplorer());
        var apiDescriptionProvider = host.App.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var descriptions = apiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(g => g.Items).ToList();

        var expectedProblems = new Dictionary<string, int[]>
        {
            { "GET:api/holdem/games/{gameId:guid}", [400, 404, 412] },
            { "POST:api/holdem/games", [400, 409, 422] },
            { "POST:api/holdem/games/{gameId:guid}/start", [400, 404, 409, 412, 422] },
            { "POST:api/holdem/games/{gameId:guid}/hands", [400, 404, 409, 412, 422] },
            { "POST:api/holdem/games/{gameId:guid}/actions", [400, 404, 409, 412, 422] },
            { "POST:api/holdem/games/{gameId:guid}/finish", [400, 404, 409, 412] },
        };

        foreach (var description in descriptions)
        {
            var key = $"{description.HttpMethod}:{description.RelativePath}";
            if (!expectedProblems.TryGetValue(key, out var expectedStatuses))
            {
                continue;
            }

            var problemResponses = description.SupportedResponseTypes
                .Where(r => r.Type == typeof(ProblemDetails))
                .Select(r => r.StatusCode)
                .OrderBy(s => s)
                .ToList();

            var expectedSorted = expectedStatuses.OrderBy(s => s).ToList();
            problemResponses.Should().Equal(expectedSorted,
                $"endpoint {key} should declare problems for statuses {string.Join(", ", expectedStatuses)}");

            foreach (var problemResponse in description.SupportedResponseTypes.Where(r => r.Type == typeof(ProblemDetails)))
            {
                problemResponse.ApiResponseFormats.Should().Contain(f => f.MediaType == "application/problem+json");
            }
        }
    }

    [TestMethod]
    public async Task ApiExplorer_BodiesAreTyped()
    {
        await using var host = await TestApiHost.StartAsync(configure: b => b.Services.AddEndpointsApiExplorer());
        var apiDescriptionProvider = host.App.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var descriptions = apiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(g => g.Items).ToList();

        var postRoutes = descriptions.Where(d => d.HttpMethod == "POST").ToList();
        postRoutes.Should().HaveCount(5, "should have 5 POST endpoints");

        var getRoutes = descriptions.Where(d => d.HttpMethod == "GET").ToList();
        getRoutes.Should().HaveCount(1, "should have 1 GET endpoint");

        foreach (var postRoute in postRoutes)
        {
            var bodyParam = postRoute.ParameterDescriptions
                .FirstOrDefault(p => p.Source == BindingSource.Body);

            if (postRoute.RelativePath?.EndsWith("finish", StringComparison.Ordinal) ?? false)
            {
                bodyParam.Should().BeNull($"endpoint {postRoute.RelativePath} should not have a body parameter");
            }
            else
            {
                bodyParam.Should().NotBeNull($"endpoint {postRoute.RelativePath} should have a body parameter");
            }
        }
    }

    [TestMethod]
    public async Task Endpoints_CarryTagSummaryAndName()
    {
        await using var host = await TestApiHost.StartAsync();
        var endpointDataSource = host.App.Services.GetRequiredService<EndpointDataSource>();
        var endpoints = endpointDataSource.Endpoints.OfType<RouteEndpoint>().Where(e => e.RoutePattern.RawText?.StartsWith("/api/holdem", StringComparison.Ordinal) ?? false).ToList();

        endpoints.Should().HaveCount(6);

        var expectedNames = new[] { "HoldemGetGame", "HoldemCreateGame", "HoldemStartGame", "HoldemStartHand", "HoldemPlay", "HoldemFinishGame" };

        foreach (var endpoint in endpoints)
        {
            var nameMetadata = endpoint.Metadata.OfType<IEndpointNameMetadata>();
            nameMetadata.Should().NotBeEmpty($"endpoint {endpoint.DisplayName} should have name metadata");
            foreach (var metadata in nameMetadata)
            {
                var name = (string?)metadata.GetType().GetProperty("EndpointName")?.GetValue(metadata);
                expectedNames.Should().Contain(name);
            }
        }
    }
}
