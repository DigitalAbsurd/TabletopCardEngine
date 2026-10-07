namespace TabletopCardEngine.Engine;

/// <summary>
/// <see cref="Street"/>'s values encode the number of community cards on the board (<c>Flop = 3</c>,
/// <c>Turn = 4</c>, <c>River = 5</c>) and are persisted in the database, so advancing a street can't
/// be done with <c>+1</c> arithmetic the way NPokerEngine's sequential <c>StreetType</c> allows. This
/// is the one place that knows the successor order and how many board cards each street shows.
/// </summary>
internal static class StreetExtensions
{
    internal static Street Next(this Street street) => street switch
    {
        Street.PreFlop => Street.Flop,
        Street.Flop => Street.Turn,
        Street.Turn => Street.River,
        Street.River => Street.Showdown,
        Street.Showdown => Street.Finished,
        _ => throw new InvalidOperationException($"{street} has no next street.")
    };

    internal static int BoardSize(this Street street) => street switch
    {
        Street.PreFlop => 0,
        Street.Flop => 3,
        Street.Turn => 4,
        Street.River or Street.Showdown or Street.Finished => 5,
        _ => throw new NotSupportedException(street.ToString())
    };
}
