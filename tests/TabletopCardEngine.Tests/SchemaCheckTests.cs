using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Data;
using TabletopCardEngine.Dto;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class SchemaCheckTests
{
    [TestMethod]
    public async Task SchemaCheck_EqualVersion_Runs()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var options = new GameOptionData { MaxPlayersCount = 2 };
        var action = HoldemAction.CreateGame(options);

        var result = await engine.ApplyHoldemActionAsync(action);

        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task SchemaCheck_NewerDatabase_Throws()
    {
        using var host = TestEngineHost.Setup(out var engine, out _, seedAction: async context =>
        {
            var schemaRow = await context.Set<TceSchemaRow>().FirstAsync(r => r.Id == 1);
            schemaRow.SchemaVersion = 2;
            await context.SaveChangesAsync();
        });

        var options = new GameOptionData { MaxPlayersCount = 2 };
        var action = HoldemAction.CreateGame(options);

        var exception = await Assert.ThrowsExceptionAsync<HoldemSchemaException>(
            () => engine.ApplyHoldemActionAsync(action));

        exception.Failure.Should().Be(HoldemSchemaFailure.DatabaseIsNewer);
        exception.Status.Should().NotBeNull();
        exception.Status!.DatabaseSchemaVersion.Should().Be(2);
    }

    [TestMethod]
    public async Task SchemaCheck_OlderDatabase_Throws()
    {
        using var host = TestEngineHost.Setup(out var engine, out _, seedAction: async context =>
        {
            var schemaRow = await context.Set<TceSchemaRow>().FirstAsync(r => r.Id == 1);
            schemaRow.SchemaVersion = 0;
            await context.SaveChangesAsync();
        });

        var options = new GameOptionData { MaxPlayersCount = 2 };
        var action = HoldemAction.CreateGame(options);

        var exception = await Assert.ThrowsExceptionAsync<HoldemSchemaException>(
            () => engine.ApplyHoldemActionAsync(action));

        exception.Failure.Should().Be(HoldemSchemaFailure.MigrationRequired);
    }

    [TestMethod]
    public async Task SchemaCheck_FailureIsNotCached()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory, seedAction: async context =>
        {
            var schemaRow = await context.Set<TceSchemaRow>().FirstAsync(r => r.Id == 1);
            schemaRow.SchemaVersion = 2;
            await context.SaveChangesAsync();
        });

        var options = new GameOptionData { MaxPlayersCount = 2 };
        var action = HoldemAction.CreateGame(options);

        await Assert.ThrowsExceptionAsync<HoldemSchemaException>(
            () => engine.ApplyHoldemActionAsync(action));

        // Fix the schema version
        await using var context = await contextFactory.CreateDbContextAsync();
        var schemaRow = await context.Set<TceSchemaRow>().FirstAsync(r => r.Id == 1);
        schemaRow.SchemaVersion = HoldemSchema.Version;
        await context.SaveChangesAsync();

        var result = await engine.ApplyHoldemActionAsync(action);
        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task SchemaCheck_ThrowsBeforeAnySignal()
    {
        using var host = TestEngineHost.Setup(out var engine, out _, seedAction: async context =>
        {
            var schemaRow = await context.Set<TceSchemaRow>().FirstAsync(r => r.Id == 1);
            schemaRow.SchemaVersion = 2;
            await context.SaveChangesAsync();
        });

        var options = new GameOptionData { MaxPlayersCount = 2 };
        var action = HoldemAction.CreateGame(options);

        // The schema check should throw before any action processing
        var exception = await Assert.ThrowsExceptionAsync<HoldemSchemaException>(
            () => engine.ApplyHoldemActionAsync(action));

        exception.Failure.Should().Be(HoldemSchemaFailure.DatabaseIsNewer);
    }
}
