using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Converters;
internal class StreetToNPokerStreetConverter : ValueConverter<Street, NPokerStreet>
{
    public StreetToNPokerStreetConverter()
        : base(street => StreetToNPokerStreet(street),
            nPokerStreet => NPokerStreetToStreet(nPokerStreet))
    {
    }

    private static NPokerStreet StreetToNPokerStreet(Street street)
        => street switch
        {
            Street.PreFlop => NPokerStreet.PREFLOP,
            Street.Flop => NPokerStreet.FLOP,
            Street.Turn => NPokerStreet.TURN,
            Street.River => NPokerStreet.RIVER,
            Street.Showdown => NPokerStreet.SHOWDOWN,
            Street.Finished => NPokerStreet.FINISHED,
            _ => throw new NotImplementedException()
        };

    private static Street NPokerStreetToStreet(NPokerStreet nPokerStreet)
        => nPokerStreet switch
        {
            NPokerStreet.PREFLOP => Street.PreFlop,
            NPokerStreet.FLOP => Street.Flop,
            NPokerStreet.TURN => Street.Turn,
            NPokerStreet.RIVER => Street.River,
            NPokerStreet.SHOWDOWN => Street.Showdown,
            NPokerStreet.FINISHED => Street.Finished,
            _ => throw new NotImplementedException()
        };
}
