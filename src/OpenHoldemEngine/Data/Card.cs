using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data
{
    internal class Card
    {
        public CardType CardType {  get; set; }
        public CardSuit CardSuit { get; set; }
    }

    public enum CardType : byte 
    { 
        Ace = 1, 
        Two = 2, 
        Three = 3, 
        Four = 4, 
        Five = 5, 
        Six = 6, 
        Seven = 7, 
        Eight = 8, 
        Nine = 9, 
        Ten = 10, 
        Jack = 11, 
        Queen = 12, 
        King = 13
    }

    public enum CardSuit : byte 
    { 
        [Description("♣")] Club = 0, 
        [Description("♦")] Diamond = 1, 
        [Description("♥")] Heart = 2, 
        [Description("♠")] Spade = 3
    }
}
