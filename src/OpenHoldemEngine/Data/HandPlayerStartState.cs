using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data
{
    internal class HandPlayerStartState
    {
        public Guid GameId { get; set; }
        public int HandNo { get; set; }
        public Hand Hand { get; set; } = new();
        public byte SeatNo { get; set; }
        public decimal StartStack { get; set; }
    }
}
