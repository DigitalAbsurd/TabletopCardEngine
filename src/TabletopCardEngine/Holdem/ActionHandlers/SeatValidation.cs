using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// Checks the seats a caller asks to fill. One method so that every handler that seats players
/// (today <c>StartHand</c>) enforces the same rules.
/// </summary>
internal static class SeatValidation
{
    internal static HoldemError? Validate(Game game, IReadOnlyList<PlayerJoinData> joinSeats)
    {
        var seen = new HashSet<byte>();
        foreach (var join in joinSeats)
        {
            var outOfRange = join.SeatNo < 1 || join.SeatNo > game.MaxPlayersCount;
            var occupied = game.Players.Any(player => player.SeatNo == join.SeatNo);
            var invalidName = string.IsNullOrWhiteSpace(join.Name) || join.Name.Length > AmountRules.PlayerNameMaxLength;
            if (outOfRange || occupied || !seen.Add(join.SeatNo) || join.BuyInAmount <= 0 || !AmountRules.HasValidScale(join.BuyInAmount) || invalidName)
            {
                return new HoldemError(HoldemErrorCode.InvalidSeat, PackResource.HoldemValidation_InvalidSeat);
            }
        }

        return null;
    }
}
