using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data
{
    internal class HandAction
    {
        public Guid GameId { get; set; }
        public byte SeatNo { get; set; }
        public byte NextSeatNo { get; set; }
        public int HandNo { get; set; }
        public Hand Hand { get; set; } = new();
        public int No { get; set; }
        public Street Street { get; set; }
        public Street NextStreet { get; set; }
        public PlayActionType PlayAction { get; set; }
        public decimal Amount { get; set; }
        public decimal AddAmount { get; set; }
        public decimal Paid { get; set; }
    }

    [DefaultValue(Street.PreFlop)] 
    public enum Street : byte 
    { 
        PreFlop = 0, 
        Flop = 3, 
        Turn = 4, 
        River = 5,
        Showdown = 6 
    }

    public enum PlayActionType : byte 
    { 
        SmallBlind = 1, 
        BigBlind = 2, 
        Fold = 3, 
        Check = 4, 
        Call = 5, 
        Raise = 6, 
        Post = 7,
        Bet = 8, 
        AllIn = 9, 
        Showdown = 100, 
        Winner = 101, 
        GameWinner = 150, 
        BuyIn = 200 
    }
}
