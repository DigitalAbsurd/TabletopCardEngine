using FluentAssertions;
using FluentAssertions.Execution;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests.Phh;

/// <summary>
/// Replays PHH hand histories through <see cref="IHoldemTable"/> and asserts that the engine
/// reproduces them. Each hand is replayed as its own game: the corpus resets stacks before every
/// hand, and a buy-in can only top a stack up, never trim it.
/// </summary>
/// <remarks>
/// Seat mapping falls out of the PHH specification: player <c>p1</c> is the small blind and the last
/// player is on the button. Mapping <c>pI</c> to seat <c>I</c> and starting the game with
/// <c>InitialDealerSeatNo = PlayerCount</c> therefore puts the button, small blind and big blind
/// exactly where the history says they are.
/// <para>
/// Heads-up the two rules cannot both hold, and the histories settle it by their action order: the
/// last player acts first pre-flop and <c>p1</c> first on every street after it, which is the button
/// posting the small blind — so heads-up it is the *last* player that posts the small blind, not
/// <c>p1</c>. See <see cref="BlindSeatsFor"/>. The engine reaches the same layout from
/// <c>InitialDealerSeatNo = PlayerCount</c> unchanged, because heads-up it moves the small blind onto
/// the button itself.
/// </para>
/// </remarks>
internal sealed class PhhReplayRunner : IDisposable
{
    /// <summary>
    /// How many hands share one database before it is thrown away and rebuilt. Every hand is its own
    /// game, so rows only ever pile up; left unbounded, a full-corpus run slows down badly as the
    /// in-memory database grows. Recycling keeps the per-hand cost flat, and hands stay independent
    /// either way.
    /// </summary>
    private const int HandsPerDatabase = 250;

    /// <summary>The five board cards a completed hand deals.</summary>
    private const int BoardCardsCount = 5;

    private readonly ICardShuffler _shuffler;
    private TestEngineHost.DisposableTestData _host = null!;
    private IHoldemTable _engine;
    private PhhHandDeck _currentDeck = null!;
    private int _handsOnCurrentDatabase;

    internal PhhReplayRunner()
    {
        _shuffler = new TestCardShuffler(
            getHoleFunc: (handNo, seatNo) => _currentDeck.HoleCards[seatNo],
            getDeckFunc: handNo => _currentDeck.BoardCards);
        _host = TestEngineHost.Setup(out _engine, out _, shuffler: _shuffler);
    }

    private void RecycleDatabaseIfNeeded()
    {
        if (_handsOnCurrentDatabase++ < HandsPerDatabase)
        {
            return;
        }

        _host.Dispose();
        _host = TestEngineHost.Setup(out _engine, out _, shuffler: _shuffler);
        _handsOnCurrentDatabase = 1;
    }

    /// <summary>
    /// Which seats post the blinds. <c>p1</c> and <c>p2</c> at three players or more; heads-up the
    /// button — the last player — posts the small blind and <c>p1</c> the big one.
    /// </summary>
    internal static (byte SmallBlindSeatNo, byte BigBlindSeatNo) BlindSeatsFor(int playerCount)
        => playerCount == 2 ? ((byte)2, (byte)1) : ((byte)1, (byte)2);

    /// <summary>
    /// The table format that suits this player count. <c>2</c> players play heads-up,
    /// <c>3..6</c> play six-max, and <c>7..10</c> play full-ring.
    /// </summary>
    internal static HoldemTableFormat TableFormatFor(int playerCount)
        => playerCount switch
        {
            2 => HoldemTableFormat.HeadsUp,
            >= 3 and <= 6 => HoldemTableFormat.SixMax,
            >= 7 and <= 10 => HoldemTableFormat.FullRing,
            _ => throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount, "Player count must be between 2 and 10"),
        };

    /// <summary>
    /// The ante the engine plays a hand's <c>antes</c> as. Every player posting the same non-zero amount is
    /// a <see cref="AnteType.Classic"/> ante; the big blind's player alone posting one is a
    /// <see cref="AnteType.BigBlind"/> ante (heads-up included, see <see cref="BlindSeatsFor"/>); all zeros is
    /// none. Any other shape is not an ante the engine has, and <paramref name="reason"/> names it.
    /// </summary>
    internal static bool AnteFor(PhhHand hand, out decimal? ante, out AnteType anteType, out string reason)
    {
        ante = null;
        anteType = AnteType.Classic;
        reason = string.Empty;

        var posted = hand.Antes.Where(value => value != 0).ToArray();
        if (posted.Length == 0)
        {
            return true;
        }

        if (posted.Length == hand.PlayerCount && posted.Distinct().Count() == 1)
        {
            ante = posted[0];
            return true;
        }

        var bigBlindIx = BlindSeatsFor(hand.PlayerCount).BigBlindSeatNo - 1;
        if (posted.Length == 1 && hand.Antes[bigBlindIx] != 0)
        {
            ante = posted[0];
            anteType = AnteType.BigBlind;
            return true;
        }

        reason = posted.Length == 1
            ? "antes on seats other than the big blind"
            : posted.Length == hand.PlayerCount
                ? "antes of different sizes"
                : "antes on some seats but not all of them and not only the big blind";
        return false;
    }

    private const string FixedLimitVariant = "FT";

    /// <summary>
    /// Not a code of the PHH specification: the synthetic pokerkit corpus names pot-limit Texas hold'em
    /// this way, by analogy with <c>NT</c> and <c>FT</c> (see <c>Resources/phh/README.md</c>).
    /// </summary>
    private const string PotLimitVariant = "PT";

    /// <summary>
    /// The betting structure a PHH variant code plays as, or <c>null</c> for a variant the engine does
    /// not play. The one place a variant becomes a structure: pot-limit adds its row here.
    /// </summary>
    internal static BettingStructure? BettingStructureFor(string variant)
        => variant switch
        {
            "NT" => BettingStructure.NoLimit,
            FixedLimitVariant => BettingStructure.FixedLimit,
            PotLimitVariant => BettingStructure.PotLimit,
            _ => null,
        };

    /// <summary>
    /// Whether the engine can model this hand at all. The engine plays hold'em with a classic or a
    /// big blind ante (see <see cref="AnteFor"/>) and a big blind of exactly twice the small blind (<c>StartHandActionHandler</c> derives
    /// it that way), and it can only settle a showdown the history wrote every card of.
    /// <para>
    /// Recorded finishing stacks are deliberately *not* required. A corpus that has them is verified
    /// against them (see <see cref="AssertHandResult"/>); one that does not is still worth replaying
    /// for the positions, the action order, the street progression and the board, which is most of
    /// what a hand history pins down.
    /// </para>
    /// </summary>
    internal static bool IsSupported(PhhHand hand, out string reason)
    {
        reason = string.Empty;

        if (BettingStructureFor(hand.Variant) is null)
        {
            reason = $"variant '{hand.Variant}' is not a Texas hold'em variant the engine plays";
        }
        else if (hand.Variant == FixedLimitVariant && (hand.SmallBet != hand.BigBlind || hand.BigBet != hand.BigBlind * 2))
        {
            reason = $"bets {hand.SmallBet}/{hand.BigBet} — the engine's fixed-limit bets are the big blind and twice it";
        }
        else if (hand.Variant == PotLimitVariant && hand.MinBet != hand.BigBlind)
        {
            reason = $"min bet {hand.MinBet} — the engine's smallest bet is the big blind";
        }
        else if (hand.Divergence is not null)
        {
            reason = $"pokerkit and the engine follow different rules here: {hand.Divergence}";
        }
        else if (hand.PlayerCount < 2)
        {
            reason = $"{hand.PlayerCount} players — a hand needs at least two";
        }
        else if (hand.BlindsOrStraddles.Length != hand.PlayerCount || hand.Antes.Length != hand.PlayerCount)
        {
            reason = "the antes and blinds do not line up with the players";
        }
        else if (!AnteFor(hand, out _, out _, out var anteReason))
        {
            reason = anteReason;
        }
        else if (hand.SmallBlind <= 0 || hand.BigBlind != hand.SmallBlind * 2)
        {
            reason = $"blinds {hand.SmallBlind}/{hand.BigBlind} — the engine derives the big blind as twice the small blind";
        }
        else if (hand.BlindsOrStraddles.Skip(2).Any(blind => blind != 0))
        {
            reason = "straddles are not supported by the engine";
        }
        else if (hand.StartingStacks.Any(stack => stack is null))
        {
            reason = "not every starting stack is recorded";
        }
        else if (!ShowdownIsFullyRecorded(hand))
        {
            reason = "a showdown is reached without every contender's cards and the whole board recorded";
        }

        return reason.Length == 0;
    }

    /// <summary>
    /// Whether a hand that ends with more than one player still in it records everything needed to
    /// settle the showdown the same way the history did. <see cref="PhhHandDeck"/> substitutes unused
    /// deck cards for whatever a history leaves out, which costs nothing on a hand decided by folds
    /// but would have the engine award the pot to somebody the finishing stacks disagree with.
    /// Corpora of bot play record every card; corpora of real play record only what was shown.
    /// </summary>
    private static bool ShowdownIsFullyRecorded(PhhHand hand)
    {
        var folded = hand.Actions.OfType<PhhFoldAction>().Select(fold => fold.PlayerIndex).ToHashSet();
        var contenders = Enumerable.Range(0, hand.PlayerCount).Where(playerIx => !folded.Contains(playerIx)).ToList();

        if (contenders.Count < 2)
        {
            return true;
        }

        var board = hand.Board;
        return board.Length == BoardCardsCount
            && board.All(card => card is not null)
            && contenders.All(playerIx => IsFullyKnown(PhhHandDeck.RecordedHoleCards(hand, playerIx)));

        static bool IsFullyKnown(Card?[] hole) => hole.Length == 2 && hole.All(card => card is not null);
    }

    /// <summary>
    /// Replays one hand and asserts the engine agrees with it. Failures are collected under an
    /// <see cref="AssertionScope"/> named after the source file. Returns the final game state so a
    /// focused test can assert more.
    /// </summary>
    internal async Task<GameData> ReplayAsync(PhhHand hand)
    {
        IsSupported(hand, out var unsupportedReason).Should()
            .BeTrue($"{hand} should be replayable, but {unsupportedReason}");

        var gameId = GameIdFor(hand.Index);
        var playerCount = hand.PlayerCount;
        _currentDeck = PhhHandDeck.Create(hand);
        RecycleDatabaseIfNeeded();

        using var scope = new AssertionScope(hand.ToString());

        AnteFor(hand, out var ante, out var anteType, out _);

        await ApplyAsync(new HoldemActionCreateGame(
            GameId: gameId,
            Options: new GameOptionData { BettingStructure = BettingStructureFor(hand.Variant)!.Value, RaiseCap = 4, TableFormat = TableFormatFor(playerCount), MaxPlayersCount = playerCount, InitialSmallBlind = hand.SmallBlind, InitialDealerSeatNo = (byte)playerCount, Ante = ante, AnteType = anteType }),
            hand);

        var gameData = await ApplyAsync(new HoldemActionStartGame(gameId,
            [.. Enumerable.Range(0, playerCount)
                .Select(playerIx => new PlayerJoinData(
                    Name: hand.Players[playerIx],
                    SeatNo: (byte)(playerIx + 1),
                    BuyInAmount: hand.StartingStacks[playerIx]!.Value))]), hand);

        AssertHandStart(gameData, hand);
        AssertAntes(gameData, hand);

        foreach (var action in hand.Actions)
        {
            // Hole and board cards reach the engine through the shuffler, and it settles the
            // showdown itself, so dealing and show/muck entries are not replayed as actions.
            if (action is not IPhhPlayerDecision decision)
            {
                continue;
            }

            var expectedSeatNo = (byte)(decision.PlayerIndex + 1);
            gameData.HandData!.NextPlayerSeatNo.Should().Be(expectedSeatNo,
                $"'{action.Raw}' should be the turn of {hand.Players[decision.PlayerIndex]} in seat {expectedSeatNo}");

            if (hand.Variant == FixedLimitVariant && decision is PhhCompleteBetRaiseAction fixedLimitRaise)
            {
                AssertFixedLimitRaiseWindow(gameData.HandData!, fixedLimitRaise);
            }

            if (hand.Variant == PotLimitVariant)
            {
                AssertPotLimitRaiseWindow(gameData.HandData!, expectedSeatNo, action);
            }

            var (actionType, size) = MapDecision(decision, gameData.HandData!);
            gameData = await ApplyAsync(new HoldemActionPlay(gameId, actionType, size), hand, action);

            AssertBoardInProgress(gameData, action);
        }

        AssertHandResult(gameData, hand);

        return gameData;
    }

    private void AssertHandStart(GameData gameData, PhhHand hand)
    {
        var handData = gameData.HandData!;

        var (smallBlindSeatNo, bigBlindSeatNo) = BlindSeatsFor(hand.PlayerCount);

        handData.DealerSeatNo.Should().Be((byte)hand.PlayerCount, "the last PHH player is on the button");
        handData.SmallBlindSeatNo.Should().Be(smallBlindSeatNo, "where the small blind sits");
        handData.BigBlindSeatNo.Should().Be(bigBlindSeatNo, "where the big blind sits");
        handData.StreetPot.Should().Be(hand.SmallBlind + hand.BigBlind, "only the blinds are in yet");
        handData.CommunityCards.Should().BeEmpty("no board is dealt pre-flop");

        for (var playerIx = 0; playerIx < hand.PlayerCount; playerIx++)
        {
            var seatNo = (byte)(playerIx + 1);
            var seatData = handData.Seats.Single(seat => seat.SeatNo == seatNo);

            seatData.Name.Should().Be(hand.Players[playerIx], $"seat {seatNo} should be taken by that player");
            seatData.StartHandStack.Should().Be(hand.StartingStacks[playerIx]!.Value, $"seat {seatNo} start stack");
            seatData.Hole.Should().Equal(_currentDeck.HoleCards[seatNo],
                $"seat {seatNo} should hold the cards the history dealt it");
        }
    }

    /// <summary>
    /// The ledger's <see cref="HandActionType.Ante"/> rows are what the history says each player posted, by
    /// seat, and <see cref="HandData.Ante"/> is the nominal ante. A post is capped at what the seat has left,
    /// which for a big blind ante is its stack after the blind.
    /// </summary>
    private static void AssertAntes(GameData gameData, PhhHand hand)
    {
        var handData = gameData.HandData!;
        AnteFor(hand, out var ante, out var anteType, out _);

        handData.Ante.Should().Be(ante ?? 0M, "the hand's nominal ante");

        var expected = new Dictionary<byte, decimal>();
        for (var playerIx = 0; playerIx < hand.PlayerCount; playerIx++)
        {
            var seatNo = (byte)(playerIx + 1);
            var stack = hand.StartingStacks[playerIx]!.Value;
            var available = anteType == AnteType.BigBlind ? stack - Math.Min(hand.BlindsOrStraddles[playerIx], stack) : stack;
            var posted = Math.Min(hand.Antes[playerIx], available);
            if (posted > 0)
            {
                expected[seatNo] = posted;
            }
        }

        handData.Actions.Where(action => action.ActionType == HandActionType.Ante)
            .ToDictionary(action => action.SeatNo, action => action.Amount).Should().BeEquivalentTo(expected,
                "each ante row should be what the history says that seat posted");
    }

    /// <summary>
    /// The engine deals the board itself as streets close, so mid-hand it can only hold a prefix of
    /// what the history recorded — and that prefix length is what <see cref="Street"/> encodes.
    /// </summary>
    private void AssertBoardInProgress(GameData gameData, PhhAction action)
    {
        var communityCards = gameData.HandData!.CommunityCards;

        communityCards.Should().Equal(_currentDeck.RecordedBoardCards.Take(communityCards.Count),
            $"the board after '{action.Raw}' should follow the history");

        if (gameData.HandData!.Street <= Street.River)
        {
            communityCards.Should().HaveCount((int)gameData.HandData!.Street,
                $"Street.{gameData.HandData!.Street} means that many board cards after '{action.Raw}'");
        }
    }

    /// <summary>
    /// In fixed-limit a bet or raise has exactly one size, so the engine must offer exactly the one
    /// the history recorded: a <c>Raise</c> window whose minimum and maximum are both that total. A
    /// replay that only applies the recorded actions could not tell the structures apart, because
    /// every fixed-limit raise is also a legal no-limit one and the pots come out the same. The check
    /// keys on the PHH variant, not on the structure the game was created with, so a game created
    /// with the wrong structure fails here.
    /// </summary>
    private static void AssertFixedLimitRaiseWindow(HandData handData, PhhCompleteBetRaiseAction raise)
    {
        var window = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);

        window.Should().NotBeNull($"'{raise.Raw}' is a fixed-limit raise, so the engine should offer one");

        if (window != null)
        {
            (window.MinValue, window.MaxValue).Should().Be((raise.Amount, raise.Amount),
                $"in fixed-limit the only raise before '{raise.Raw}' is the recorded total");
        }
    }

    /// <summary>
    /// In pot-limit the top of the <c>Raise</c> window is the standing wager plus the pot after the seat
    /// calls, capped at the seat's stack plus its street wager. The runner works that out from the
    /// table's own numbers (every chip committed this hand, blinds and folded seats' wagers included)
    /// and does not read it back from the engine. Replaying the recorded actions cannot tell pot-limit
    /// from no-limit, because every pot-limit raise is also a legal no-limit one and the pots come out
    /// the same. The check keys on the PHH variant, not on the structure the game was created with.
    /// </summary>
    private static void AssertPotLimitRaiseWindow(HandData handData, byte seatNo, PhhAction action)
    {
        var window = handData.Evaluations.SingleOrDefault(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);

        if (window == null)
        {
            return;
        }

        var seat = handData.Seats.Single(candidate => candidate.SeatNo == seatNo);
        var ownWager = seat.BetSize ?? 0M;
        var standingWager = handData.Seats.Max(candidate => candidate.BetSize ?? 0M);
        var call = standingWager - ownWager;
        var potAfterCall = handData.Pot + handData.StreetPot + call;
        var potLimit = Math.Min(standingWager + potAfterCall, seat.Stack + ownWager);

        window.MaxValue.Should().Be(potLimit,
            $"the pot-limit maximum of the raise before '{action.Raw}' is the standing wager {standingWager} plus the pot {potAfterCall} after a call of {call}, capped at the stack");
    }

    private void AssertHandResult(GameData gameData, PhhHand hand)
    {
        var handData = gameData.HandData!;

        handData.CommunityCards.Should().Equal(_currentDeck.RecordedBoardCards,
            "the finished hand should have dealt exactly the board the history recorded, running it out in full when betting ended early");

        for (var playerIx = 0; playerIx < hand.PlayerCount; playerIx++)
        {
            var seatNo = (byte)(playerIx + 1);
            var seatData = handData.Seats.Single(seat => seat.SeatNo == seatNo);

            seatData.HandResult.Should().NotBeNull($"seat {seatNo} should have a result once the hand is over");

            // Not every corpus records where the stacks landed. The handhq hands do not, and could
            // not be compared exactly even if they did: their pots are raked, which the engine has no
            // concept of. Those hands are replayed for everything else a history pins down.
            // A corpus without finishing stacks may carry each player's net result instead (`_results`).
            var expectedFinish = hand.FinishingStacks is { } finishingStacks
                ? finishingStacks[playerIx]
                : hand.Results is { } results ? hand.StartingStacks[playerIx]!.Value + results[playerIx] : (decimal?)null;

            if (expectedFinish is { } finish)
            {
                (seatData.StartHandStack + (seatData.HandResult?.Net ?? 0)).Should()
                    .Be(finish, $"seat {seatNo} finish stack");
            }
        }

        handData.Seats.Sum(seat => seat.HandResult?.Net ?? 0).Should()
            .Be(0, "a hand neither creates nor destroys chips");
    }

    private static (PlayerActionType ActionType, decimal Size) MapDecision(IPhhPlayerDecision decision, HandData handData)
        => decision switch
        {
            PhhFoldAction => (PlayerActionType.Fold, 0M),

            // `cc` covers both checking and calling; which one applies is a property of the position,
            // and the engine has already worked it out in Evaluations.
            PhhCheckCallAction => (
                handData.Evaluations.Any(evaluation => evaluation.PlayerActionType == PlayerActionType.Check)
                    ? PlayerActionType.Check
                    : PlayerActionType.Call,
                0M),

            // `cbr` names the total the street wager is brought up to, which is exactly what the
            // engine passes through as the raise amount (PlayActionHandler.CalculateBetAmount).
            PhhCompleteBetRaiseAction raise => (PlayerActionType.Raise, raise.Amount),

            _ => throw new NotSupportedException($"'{((PhhAction)decision).Raw}' is not a mappable player decision")
        };

    private async Task<GameData> ApplyAsync(HoldemAction holdemAction, PhhHand hand, PhhAction? phhAction = null)
    {
        var result = await _engine.ApplyAsync(holdemAction);

        if (result.IsFailed)
        {
            throw new PhhReplayException(
                $"{hand}: the engine rejected {holdemAction.GetType().Name}" +
                (phhAction == null ? string.Empty : $" for '{phhAction.Raw}'") +
                $" — {string.Join("; ", result.Errors.Select(error => error.Message))}");
        }

        return result.Value;
    }

    /// <summary>A stable per-hand game id, so a failing hand can be found in a dumped database.</summary>
    private static Guid GameIdFor(int handIndex) => new(handIndex, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);

    public void Dispose() => _host.Dispose();
}

/// <summary>Thrown when the engine refuses an action the history says was played.</summary>
internal sealed class PhhReplayException(string message) : Exception(message);
