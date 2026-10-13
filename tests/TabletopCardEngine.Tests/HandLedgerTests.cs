using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The hand's full ledger of actions, exposed as <see cref="HandData.Actions"/>.
/// </summary>
[TestClass]
public sealed class HandLedgerTests
{
    private const decimal SmallBlind = 5M;
    private const decimal Stack = 100M;

    [TestMethod]
    public async Task HandLedger_IncludesAllActionsInOrder()
    {
        var gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");
        var gameOptions = new GameOptionData { MaxPlayersCount = 6, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = 1 };

        var seatsData = new PlayerJoinData[]
        {
            new("Player1", 1, Stack),
            new("Player2", 2, Stack),
            new("Player3", 3, Stack),
            new("Player4", 4, Stack),
            new("Player5", 5, Stack),
            new("Player6", 6, Stack)
        };

        var holeCards = new Dictionary<byte, Card[]>()
        {
            { 1, CardsFrom("7h", "td") },
            { 2, CardsFrom("8s", "2s") },
            { 3, CardsFrom("4h", "9d") },
            { 4, CardsFrom("jh", "kd") },
            { 5, CardsFrom("7s", "5d") },
            { 6, CardsFrom("3h", "ts") },
        };

        var deckCards = CardsFrom("ac", "ad", "as", "ah", "2d");

        using (TestEngineHost.Setup(out var holdemEngine, out _,
                shuffler: new TestCardShuffler(getHoleFunc: (no, seatNo) => holeCards[seatNo], getDeckFunc: handNo => deckCards)))
        {
            // Create and start the game
            await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionCreateGame(GameId: gameId, Options: gameOptions));

            var gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionStartGame(GameId: gameId, JoinSeats: seatsData));

            AssertActionsIntegrity(gameData.Value);

            // First action: Player 6 calls the big blind
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
            AssertActionsIntegrity(gameData.Value);

            // Second action: Player 1 folds
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Fold, 0));
            AssertActionsIntegrity(gameData.Value);

            // Third action: Player 2 raises to 15
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Raise, 15));
            AssertActionsIntegrity(gameData.Value);

            // Fourth action: Player 3 calls
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
            AssertActionsIntegrity(gameData.Value);

            // Fifth action: Player 4 folds
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Fold, 0));
            AssertActionsIntegrity(gameData.Value);

            // Sixth action: Player 5 calls
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
            AssertActionsIntegrity(gameData.Value);

            // Seventh action: Player 6 calls
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Call, 0));
            AssertActionsIntegrity(gameData.Value);

            // Eighth action: Small blind checks
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Check, 0));
            AssertActionsIntegrity(gameData.Value);

            // Ninth action: Big blind checks
            gameData = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionPlay(gameId, PlayerActionType.Check, 0));
            AssertActionsIntegrity(gameData.Value);
        }
    }

    private static void AssertActionsIntegrity(GameData gameData)
    {
        var handData = gameData.HandData;
        handData.Should().NotBeNull();

        var actions = handData!.Actions;

        // Actions count must match the number of HandAction rows
        (actions.Count >= 2).Should().BeTrue("at least the two blinds");

        // No values must be strictly ascending
        var nos = actions.Select(a => a.No).ToList();
        for (int i = 0; i < nos.Count - 1; i++)
        {
            nos[i].Should().BeLessThan(nos[i + 1], $"action No values must be strictly ascending");
        }

        // Last action must match GetLastAction()
        if (actions.Count > 0)
        {
            var lastAction = actions[^1];
            var expectedLastAction = handData.GetLastAction();
            lastAction.Should().Be(expectedLastAction, "the last action in Actions should equal GetLastAction()");
        }

        // For every seat with a LastAction, it should match the last element of Actions with that SeatNo
        foreach (var seat in handData.Seats.Where(s => s.LastAction != null))
        {
            var lastActionForSeat = actions.Where(a => a.SeatNo == seat.SeatNo).LastOrDefault();
            lastActionForSeat.Should().Be(seat.LastAction, $"seat {seat.SeatNo}'s last action should match");
        }

        // First two actions should be SmallBlind and BigBlind
        if (actions.Count >= 2)
        {
            var firstAction = actions[0];
            var secondAction = actions[1];

            firstAction.ActionType.Should().Be(HandActionType.SmallBlind);
            firstAction.SeatNo.Should().Be(handData.SmallBlindSeatNo);

            secondAction.ActionType.Should().Be(HandActionType.BigBlind);
            secondAction.SeatNo.Should().Be(handData.BigBlindSeatNo);
        }
    }

    private static Card[] CardsFrom(params string[] cardStrings)
        => [.. cardStrings.Select(Card.Parse)];
}
