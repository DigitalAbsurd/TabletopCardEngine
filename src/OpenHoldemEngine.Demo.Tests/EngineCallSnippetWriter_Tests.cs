using FluentAssertions;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Tests;

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
            Options: new GameOptionData(
                MaxPlayersCount: 6,
                InitialSmallBlind: 50m,
                InitialDealerSeatNo: 0
            )
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
        snippet.Should().Contain("MaxPlayersCount: 6");
        snippet.Should().Contain("InitialSmallBlind: 50m");
        snippet.Should().Contain("var result = await engine.ApplyHoldemActionAsync(");
        snippet.Should().Contain(";");
    }

    [TestMethod]
    public void GenerateSnippet_PlayerAction_Fold_ProducesValidCSharp()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionPlayerAction(
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
        snippet.Should().Contain("new HoldemActionPlayerAction(");
        snippet.Should().Contain("PlayerActionType.Fold");
        snippet.Should().Contain("Size: 0m");
        snippet.Should().Contain(");");
    }

    [TestMethod]
    public void GenerateSnippet_PlayerAction_Raise_ProducesValidCSharp()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionPlayerAction(
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
            Options: new GameOptionData(
                MaxPlayersCount: 2,
                InitialSmallBlind: 50m,
                InitialDealerSeatNo: 0
            )
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
            Options: new GameOptionData(
                MaxPlayersCount: 2,
                InitialSmallBlind: 50m,
                InitialDealerSeatNo: 0
            )
        );
        journal.RecordSuccess(createAction);

#pragma warning disable CA1861
        var joinSeats = new[]
        {
            new PlayerJoinData(SeatNo: 1, Name: "Alice", BuyInAmount: 1000m),
            new PlayerJoinData(SeatNo: 2, Name: "Bob", BuyInAmount: 1000m),
        };
#pragma warning restore CA1861
        var startAction = new HoldemActionStartHand(GameId: gameId) { JoinSeats = joinSeats };
        journal.RecordSuccess(startAction);

        // Act
        var snippet = EngineCallSnippetWriter.GenerateWholeSessionSnippet(journal);

        // Assert
        snippet.Should().Contain("using Microsoft.Extensions.DependencyInjection;");
        snippet.Should().Contain("using OpenHoldemEngine;");
        snippet.Should().Contain("using OpenHoldemEngine.Dto;");
        snippet.Should().Contain("using OpenHoldemEngine.Data;");
        snippet.Should().Contain("var services = new ServiceCollection();");
        snippet.Should().Contain("services.AddHoldemEngine(");
        snippet.Should().Contain("DataSource=:memory:");
        snippet.Should().Contain("var engine = provider.GetRequiredService<HoldemEngine>();");
        snippet.Should().Contain("new HoldemActionCreateGame(");
        snippet.Should().Contain("new HoldemActionStartHand(");
    }

    [TestMethod]
    public void GenerateResultSummary_SuccessfulAction_ReturnsSuccess()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var action = new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData(
                MaxPlayersCount: 2,
                InitialSmallBlind: 50m,
                InitialDealerSeatNo: 0
            )
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
            Options: new GameOptionData(
                MaxPlayersCount: 2,
                InitialSmallBlind: 50m,
                InitialDealerSeatNo: 0
            )
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
        var action = new HoldemActionPlayerAction(
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
