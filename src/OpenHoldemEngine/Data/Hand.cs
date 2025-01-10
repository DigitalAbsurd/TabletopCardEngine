using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data
{
    internal class Hand
    {
        public Guid GameId { get; set; }
        public Game? Game { get; set; }
        public int No { get; set; }
        public List<HandDeckCard> DeckCards { get; set; } = new();
    }
}
