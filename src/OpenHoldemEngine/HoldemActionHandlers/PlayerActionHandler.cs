using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Engine;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class PlayerActionHandler(ICardShuffler? cardShuffler) : HoldemActionHandler<HoldemActionPlayerAction>
{
    internal override async Task<Result> ImplementHandleAsync(HoldemActionPlayerAction holdemAction, Game game)
    {
        if (game.IsCurrentHandEnded())
        {
            return Result.Fail(PackResource.HoldemValidation_CurrentHandEnded);
        }

        var hand = game.GetCurrentHand();
        var table = HandTableFactory.Build(game, await BuildDeckAsync());

        var seatNo = table.NextSeatNo;
        var seat = table[seatNo];
        var window = BettingRules.LegalActions(table, seatNo);

        var outcome = HandRunner.ApplyAction(table, holdemAction.ActionType, CalculateBetAmount());

        game.AddAction(
            seatNo: outcome.SeatNo,
            street: outcome.Street,
            playerActionType: holdemAction.ActionType,
            size: holdemAction.Size,
            amount: outcome.Amount,
            addAmount: outcome.AddAmount,
            nextSeatNo: outcome.NextSeatNo,
            nextStreet: outcome.NextStreet,
            paid: outcome.Paid);

        foreach (var card in outcome.Board)
        {
            game.AddDeckCard(card);
        }

        if (outcome.PrizeBySeatNo is { } prizes)
        {
            var board = game.GetCurrentHand().GetDeckCards();
            var handEndStates = hand.PlayerStartStates.Select(startState =>
            {
                var hole = table.Find(startState.SeatNo)?.Hole ?? [];
                var seatBet = hand.GetSeatBetAmount(startState.SeatNo);
                var prize = prizes.GetValueOrDefault(startState.SeatNo);

                // Computed directly as prize - bet, not as (StartStack - bet + prize) - StartStack:
                // a deep stack (many integer digits) added to a prize with a long fractional tail
                // (an uneven pot split can need most of decimal's ~28-29 significant digits) can force
                // decimal to round the fraction away, and the two seats in a hand rarely have the same
                // number of stack digits — so the rounding wouldn't even land the same way on both
                // sides, and Sum(Net) across the hand would stop being exactly zero.
                var winAmount = prize - seatBet;

                return new HandPlayerEndState
                {
                    GameId = holdemAction.GameId,
                    HandNo = hand.No,
                    Hand = hand,
                    SeatNo = startState.SeatNo,
                    FinishStack = startState.StartStack + winAmount,
                    WinAmount = winAmount,
                    Strength = HandEvaluator.EvaluateStrength(hole, board),
                    RankType = HandEvaluator.EvaluateRankType(hole, board),
                };
            });
            game.AddHandEndStates(handEndStates);
        }

        return Result.Ok();

        decimal CalculateBetAmount()
            => holdemAction.ActionType switch
            {
                PlayerActionType.Check => seat.StreetWager,
                PlayerActionType.Call => window.CallTo,
                PlayerActionType.Fold => 0M,
                PlayerActionType.Raise => holdemAction.Size,
                PlayerActionType.Bet => holdemAction.Size + seat.StreetWager,
                _ => throw new NotImplementedException()
            };

        async Task<CardDeck?> BuildDeckAsync()
        {
            if (cardShuffler == null || (byte)hand.GetStreet() >= 5)
            {
                return null;
            }
            var deckCards = await cardShuffler.GetDeckCardsAsync(holdemAction.GameId, hand.No);
            var remaining = deckCards.Except(hand.DeckCards.Select(deckCard => deckCard.Card));
            return CardDeck.Predetermined(remaining);
        }
    }
}
