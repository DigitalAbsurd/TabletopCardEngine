using FluentAssertions;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Tests;

[TestClass]
public sealed class HoldemEngineTests
{
    [TestMethod]
    public async Task HoldemAction_StartGame()
    {
        var gameId = Guid.Parse("{11111111-1111-1111-1111-111111111111}");
        var gameOptions = new GameOptionData(
                MaxPlayersCount: 10,
                InitialSmallBlind: 5,
                InitialDealerSeatNo: 3);

        var seatsData = new PlayerJoinData[]
        {
            new("Junpei", 1, 100),
            new("Geraldine", 2, 100),
            new("Zuhaitz", 3, 100),
            new("Ozias", 4, 100),
            new("Ilona", 5, 100),
            new("Broen", 6, 100)
        };

        var holeCards = new Dictionary<byte, Card[]>()
        {
            { 1, CardsFromStrings([ "7h", "td" ]) },
            { 2, CardsFromStrings([ "8s", "2s" ]) },
            { 3, CardsFromStrings([ "4h", "9d" ]) },
            { 4, CardsFromStrings([ "jh", "kd" ]) },
            { 5, CardsFromStrings([ "7s", "5d" ]) },
            { 6, CardsFromStrings([ "3h", "ts" ]) },
        };

        var deckCards = CardsFromStrings(["ac", "ad", "as", "ah", "2d"]);

        var expectedGameData = new GameData(
            GameId: gameId,
            Options: gameOptions,
            HandData: new HandData(
                No: 1,
                Seats:
                [
                    new(1, "Junpei", 100, 100)
                    {
                        Hole = holeCards[1]
                    },
                    new(2, "Geraldine", 100, 100)
                    {
                        Hole = holeCards[2]
                    },
                    new(3, "Zuhaitz", 100, 100)
                    {
                        Hole = holeCards[3]
                    },
                    new(4, "Ozias", 100, 95)
                    {
                        Hole = holeCards[4],
                        LastAction = new PlayerActionData(Street.PreFlop, PlayerActionType.SmallBlind, 5, 1)
                    },
                    new(5, "Ilona", 100, 90)
                    {
                        Hole = holeCards[5],
                        LastAction = new PlayerActionData(Street.PreFlop, PlayerActionType.BigBlind, 10, 2)
                    },
                    new(6, "Broen", 100, 100)
                    {
                        Hole = holeCards[6]
                    },
                ],
                DealerSeatNo: 3,
                SmallBlindSeatNo: 4,
                BigBlindSeatNo: 5,
                NextPlayerSeatNo: 6,
                Street: Street.PreFlop,
                StreetPot: 15,
                Pot: 0,
                CommunityCards: [],
                Evaluations:
                [
                    new(PlayerActionType.Fold),
                    new(PlayerActionType.Call, 10, 10),
                    new(PlayerActionType.Raise, 15, 100)
                ])
            );

        GameData actualGameData;
        using (TestEngineHost.Setup(out var holdemEngine, out _,
                shuffler: new TestCardShuffler(getHoleFunc: (no, seatNo) => holeCards[seatNo], getDeckFunc: handNo => deckCards)))
        {
            await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionCreateGame(
                    GameId: gameId,
                    Options: gameOptions));

            actualGameData = (await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionStartHand(GameId: gameId)
                {
                    JoinSeats = seatsData
                })).Value;
        }

        actualGameData.ShouldAsJToken().BeEquivalentToAsJToken(expectedGameData);
    }

    [TestMethod]
    public async Task HoldemAction_StartGame_ValidationAlreadyExists()
    {
        var gameId = Guid.Parse("{11111111-1111-1111-1111-111111111111}");
        var gameOptions = new GameOptionData(
                MaxPlayersCount: 10,
                InitialSmallBlind: 5,
                InitialDealerSeatNo: 3);

        HoldemResult<GameData> failResult;

        using (TestEngineHost.Setup(out var holdemEngine, out _))
        {
            await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionCreateGame(
                    GameId: gameId,
                    Options: gameOptions));

            failResult = await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionCreateGame(
                    GameId: gameId,
                    Options: gameOptions));
        }

        failResult.IsFailed.Should().BeTrue();
    }

    private static Card[] CardsFromStrings(params string[] cardStrings)
    {
        var cardToStringConverter = new Converters.CardToStringConverter();

        if (cardStrings == null)
        {
            return [];
        }

        return cardStrings.Select(str => (Card)cardToStringConverter.ConvertFromProvider(new string([.. str.Reverse()]))!).ToArray()!;
    }
}
