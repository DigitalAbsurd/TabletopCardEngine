using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.AspNetCore.Http;
using System.Text.Json;

namespace OpenHoldemEngine.AspNetCore.Tests;

/// <summary>
/// Tests for <see cref="HoldemProblems"/>, which maps error codes to HTTP statuses and problem details.
/// </summary>
[TestClass]
public sealed class HoldemProblemsTests
{
    [TestMethod]
    [DataRow(HoldemErrorCode.GameAlreadyExists, 409)]
    [DataRow(HoldemErrorCode.CurrentHandEnded, 409)]
    [DataRow(HoldemErrorCode.PreviousHandNotEnded, 409)]
    [DataRow(HoldemErrorCode.InvalidAction, 422)]
    [DataRow(HoldemErrorCode.UnsupportedGameOptions, 422)]
    [DataRow(HoldemErrorCode.InvalidGameOptions, 422)]
    [DataRow(HoldemErrorCode.InvalidSeat, 422)]
    [DataRow(HoldemErrorCode.GameAlreadyStarted, 409)]
    [DataRow(HoldemErrorCode.GameNotStarted, 409)]
    [DataRow(HoldemErrorCode.GameFinished, 409)]
    [DataRow(HoldemErrorCode.Conflict, 412, true)] // Conflict with If-Match -> 412
    [DataRow(HoldemErrorCode.Conflict, 409, false)] // Conflict without If-Match -> 409
    [DataRow(HoldemErrorCode.GameNotFound, 404)]
    [DataRow(HoldemErrorCode.IllegalAction, 422)]
    public void StatusCodeFor_EveryCode_MatchesTheTable(HoldemErrorCode code, int expectedStatus, bool ifMatchPresent = false)
    {
        var status = HoldemProblems.StatusCodeFor(code, ifMatchPresent);
        status.Should().Be(expectedStatus, because: $"{code} should map to {expectedStatus}");
    }

    [TestMethod]
    public void StatusCodeFor_CoversEveryDefinedCode()
    {
        foreach (var code in Enum.GetValues<HoldemErrorCode>())
        {
            foreach (var ifMatchPresent in new[] { false, true })
            {
                var status = HoldemProblems.StatusCodeFor(code, ifMatchPresent);
                status.Should().BeOneOf(404, 409, 412, 422);
            }
        }
    }

    [TestMethod]
    public async Task ToProblem_WritesProblemJsonWithTheCode()
    {
        var error = new HoldemError(HoldemErrorCode.Conflict, "The game was changed by another action.");
        var errors = new[] { error };

        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        context.Response.Body = new MemoryStream();

        var problem = HoldemProblems.ToProblem(errors, ifMatchPresent: false);
        await problem.ExecuteAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(409);
        root.GetProperty("detail").GetString().Should().Be("The game was changed by another action.");
        root.GetProperty("code").GetString().Should().Be("Conflict");
        root.GetProperty("type").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Docs_ErrorsTable_ListsEveryCode()
    {
        var docsFile = Path.Combine(RepositoryRoot.FullPath, "docs", "aspnetcore.md");
        File.Exists(docsFile).Should().BeTrue();

        var content = File.ReadAllText(docsFile);

        foreach (var code in Enum.GetValues<HoldemErrorCode>())
        {
            var codeName = code.ToString();
            content.Should().Contain($"`{codeName}`",
                because: $"the docs must list the {codeName} error code");

            // Verify the status is documented for this code
            if (code == HoldemErrorCode.Conflict)
            {
                // Conflict has two possible statuses
                content.Should().Contain("412");
                content.Should().Contain("409");
            }
            else if (code == HoldemErrorCode.GameNotFound)
            {
                content.Should().Contain("404",
                    because: $"the docs must list status 404 for {codeName}");
            }
            else if (code is HoldemErrorCode.InvalidAction or HoldemErrorCode.UnsupportedGameOptions or
                HoldemErrorCode.InvalidGameOptions or HoldemErrorCode.InvalidSeat or
                HoldemErrorCode.IllegalAction)
            {
                content.Should().Contain("422",
                    because: $"the docs must list status 422 for {codeName}");
            }
            else
            {
                content.Should().Contain("409",
                    because: $"the docs must list status 409 for {codeName}");
            }
        }
    }
}
