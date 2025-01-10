using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data
{
    internal class HandDeckCard : Card
    {
        public Guid GameId { get; set; }
        public int HandNo { get; set; }
        public Hand Hand { get; set; } = new();
        public byte Sequence { get; set; }
    }
}
