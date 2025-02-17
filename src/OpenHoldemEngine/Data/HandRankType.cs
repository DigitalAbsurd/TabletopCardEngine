using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data;
public enum HandRankType : byte
{
    None = 0,
    HighCard = 1,
    Pair = 2,
    TwoPair = 3,
    Trips = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9
}
