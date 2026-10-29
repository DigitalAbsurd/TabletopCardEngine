using FluentAssertions;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class EngineCallSnippetWriter_Tests
{
    [TestMethod]
    public void GenerateSnippet_CreateGame_ProducesValidCSharp()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { TableFormat = HoldemTableFormat.SixMax, MaxPlayersCount = 6, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 }
        );
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: action,
            Succeeded: true,
            ErrorMessages: []
        );

        // Act
        var snippet = EngineCallSnippetWriter.GenerateSnippet(entry);

        // Assert
        snippet.Should().Contain("new HoldemActionCreateGame(");
        snippet.Should().Contain("TableFormat = HoldemTableFormat.SixMax");
        snippet.Should().Contain("MaxPlayersCount = 6");
        snippet.Should().Contain("InitialSmallBlind = 50m");
        snippet.Should().Contain("var result = await engine.ApplyAsync(");
        snippet.Should().Contain(";");
    }

    [TestMethod]
    [DataRow(true, DisplayName = "Succeeded")]
    [DataRow(false, DisplayName = "Failed")]
    public void GenerateSnippet_AvailableActions_OnlyAfterASuccess(bool succeeded)
    {
        // Arrange
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: new HoldemActionPlay(GameId: Guid.NewGuid(), ActionType: PlayerActionType.Call, Size: 0),
            Succeeded: succeeded,
            ErrorMessages: succeeded ? [] : ["The current hand is completed"]);

        // Act
        var lines = EngineCallSnippetWriter.GenerateSnippet(entry).Split(Environment.NewLine);

        // Assert - right after the call, and only when result.Value can be read
        const string availableActions = "var availableActions = result.Value.HandData?.Evaluations;";
        if (succeeded)
        {
            lines.Should().Contain(availableActions);
            lines[Array.IndexOf(lines, availableActions) - 1].Should().Be(");");
        }
        else
        {
            lines.Should().NotContain(line => line.Contains("availableActions", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void GenerateSnippet_PlayerAction_Fold_ProducesValidCSharp()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionPlay(
            GameId: gameId,
            ActionType: PlayerActionType.Fold,
            Size: 0
        );
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: action,
            Succeeded: true,
            ErrorMessages: []
        );

        // Act
        var snippet = EngineCallSnippetWriter.GenerateSnippet(entry);

        // Assert
        snippet.Should().Contain("new HoldemActionPlay(");
        snippet.Should().Contain("PlayerActionType.Fold");
        snippet.Should().Contain("Size: 0m");
        snippet.Should().Contain(");");
    }

    [TestMethod]
    public void GenerateSnippet_PlayerAction_Raise_ProducesValidCSharp()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionPlay(
            GameId: gameId,
            ActionType: PlayerActionType.Raise,
            Size: 300m
        );
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: action,
            Succeeded: true,
            ErrorMessages: []
        );

        // Act
        var snippet = EngineCallSnippetWriter.GenerateSnippet(entry);

        // Assert
        snippet.Should().Contain("PlayerActionType.Raise");
        snippet.Should().Contain("Size: 300m");
    }

    [TestMethod]
    public void GenerateSnippet_FailedAction_IncludesErrorMessage()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 }
        );
#pragma warning disable CA1861
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: action,
            Succeeded: false,
            ErrorMessages: new[] { "The game with passed Id already exists " }
        );
#pragma warning restore CA1861

        // Act
        var snippet = EngineCallSnippetWriter.GenerateSnippet(entry);

        // Assert
        snippet.Should().Contain("// Error:");
        snippet.Should().Contain("The game with passed Id already exists");
    }

    [TestMethod]
    public void GenerateWholeSessionSnippet_CompilableProgram()
    {
        // Arrange
        var journal = new EngineCallJournal();
        var gameId = Guid.NewGuid();

        var createAction = new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 }
        );
        journal.RecordSuccess(createAction);

#pragma warning disable CA1861
        var joinSeats = new[]
        {
            new PlayerJoinData(SeatNo: 1, Name: "Alice", BuyInAmount: 1000m),
            new PlayerJoinData(SeatNo: 2, Name: "Bob", BuyInAmount: 1000m),
        };
#pragma warning restore CA1861
        var startAction = new HoldemActionStartGame(GameId: gameId, JoinSeats: joinSeats);
        journal.RecordSuccess(startAction);

        // Act
        var snippet = EngineCallSnippetWriter.GenerateWholeSessionSnippet(journal);

        // Assert
        snippet.Should().Contain("using Microsoft.Extensions.DependencyInjection;");
        snippet.Should().Contain("using TabletopCardEngine;");
        snippet.Should().Contain("using TabletopCardEngine.Holdem;");
        snippet.Should().Contain("using TabletopCardEngine.Holdem.Dto;");
        snippet.Should().Contain("using TabletopCardEngine.Sqlite;");
        snippet.Should().NotContain("using TabletopCardEngine.Holdem.Data;");
        snippet.Should().Contain("var services = new ServiceCollection();");
        snippet.Should().Contain("services.AddTabletopCardEngine(tce => tce.AddHoldem(");
        snippet.Should().Contain("    configure: null));");
        snippet.Should().Contain("opts => opts.UseHoldemSqlite(\"DataSource=:memory:\")");
        snippet.Should().NotContain("opts.UseSqlite");
        snippet.Should().Contain("await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();");
        snippet.IndexOf("BuildServiceProvider()", StringComparison.Ordinal).Should().BeLessThan(
            snippet.IndexOf("MigrateAsync()", StringComparison.Ordinal));
        snippet.IndexOf("MigrateAsync()", StringComparison.Ordinal).Should().BeLessThan(
            snippet.IndexOf("var engine", StringComparison.Ordinal));
        snippet.Should().Contain("var engine = provider.GetRequiredService<IHoldemTable>();");
        snippet.Should().Contain("new HoldemActionCreateGame(");
        snippet.Should().Contain("new HoldemActionStartGame(");
    }

    [TestMethod]
    public void GenerateResultSummary_SuccessfulAction_ReturnsSuccess()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 }
        );
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: action,
            Succeeded: true,
            ErrorMessages: []
        );

        // Act
        var summary = EngineCallSnippetWriter.GenerateResultSummary(entry);

        // Assert
        summary.Should().Be("Success");
    }

    [TestMethod]
    public void GenerateResultSummary_FailedAction_ReturnsErrorMessage()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 }
        );
        var errorMsg = "The game with passed Id already exists ";
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: action,
            Succeeded: false,
            ErrorMessages: new[] { errorMsg }
        );

        // Act
        var summary = EngineCallSnippetWriter.GenerateResultSummary(entry);

        // Assert
        summary.Should().Be($"Error: {errorMsg}");
    }

    [TestMethod]
    public void GenerateSnippet_DecimalFormatting_UsesInvariantCulture()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionPlay(
            GameId: gameId,
            ActionType: PlayerActionType.Raise,
            Size: 123.45m
        );
        var entry = new EngineCallJournalEntry(
            SequenceNumber: 1,
            Action: action,
            Succeeded: true,
            ErrorMessages: []
        );

        // Act
        var snippet = EngineCallSnippetWriter.GenerateSnippet(entry);

        // Assert
        snippet.Should().Contain("123.45m");
        snippet.Should().NotContain("123,45");
    }
}
