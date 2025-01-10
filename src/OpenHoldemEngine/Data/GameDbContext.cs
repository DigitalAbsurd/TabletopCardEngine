using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHoldemEngine.Data
{
    internal class GameDbContext : DbContext
    {
        public DbSet<Game> Games { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
            .HasKey(e => new { e.GameId, e.SeatNo, e.RoundNoStart });

            modelBuilder.Entity<BuyIn>()
            .HasKey(e => new { e.GameId, e.SeatNo, e.HandNo });

            modelBuilder.Entity<Hand>()
            .HasKey(e => new { e.GameId, e.No });

            modelBuilder.Entity<HandPlayerStartState>()
             .HasKey(e => new { e.GameId, e.HandNo, e.SeatNo });

            modelBuilder.Entity<HandPlayerEndState>()
             .HasKey(e => new { e.GameId, e.HandNo, e.SeatNo });

            modelBuilder.Entity<Card>()
            .Property(card => card.CardType)
            .HasConversion<string>();

            modelBuilder.Entity<Card>()
            .Property(card => card.CardSuit)
            .HasConversion<string>();

            modelBuilder.Entity<HandDeckCard>()
                .HasKey(e => new { e.GameId, e.HandNo, e.Sequence });

            modelBuilder.Entity<HandPlayerCard>()
            .HasKey(e => new { e.GameId, e.HandNo, e.SeatNo, e.Sequence });

            modelBuilder.Entity<HandAction>()
            .HasKey(e => new { e.GameId, e.HandNo, e.SeatNo, e.No });
        }
    }
}
