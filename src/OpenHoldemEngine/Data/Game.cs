using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data
{
    internal class Game
    {
        [Key] public Guid Id { get; set; }
        public decimal InitialSmallBlind { get; set; } = 1;
        public int AutoIncrementHands { get; set; } = 0;
        public int MaxPlayersCount { get; set; } = 6;
        public int MaxHandsCount { get; set; } = 0;
        public bool AllowRebuy { get; set; } = false;
    }
}
