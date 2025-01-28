using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Converters;

namespace OpenHoldemEngine.Data;

public class HoldemDbContext : DbContext
{
    public HoldemDbContext(DbContextOptions dbContextOptions)
        : base(dbContextOptions) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>()
            .HasMany(g => g.Hands)
            .WithOne(h => h.Game)
            .HasForeignKey(h => h.GameId);

        modelBuilder.Entity<Game>()
            .HasMany(g => g.BuyIns)
            .WithOne(p => p.Game)
            .HasForeignKey(p => p.GameId);

        modelBuilder.Entity<Hand>()
            .HasMany(g => g.Actions)
            .WithOne(h => h.Hand)
            .HasForeignKey(act => new { act.GameId, act.HandNo });

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

        modelBuilder.Entity<Hand>()
            .HasMany(h => h.PlayerStartStates)
            .WithOne(hss => hss.Hand)
            .HasForeignKey(hss => new { hss.GameId, hss.HandNo })
            .IsRequired();

        modelBuilder.Entity<Hand>()
            .HasMany(h => h.PlayerEndStates)
            .WithOne(hes => hes.Hand)
            .HasForeignKey(hes => new { hes.GameId, hes.HandNo })
            .IsRequired();

        modelBuilder.Entity<HandDeckCard>()
            .Property(e => e.Card)
            .HasConversion<CardToStringConverter>()
            .HasMaxLength(2);

        modelBuilder.Entity<HandHoleCard>()
            .Property(e => e.Card)
            .HasConversion<CardToStringConverter>()
            .HasMaxLength(2);

        modelBuilder.Entity<HandDeckCard>()
            .HasKey(e => new { e.GameId, e.HandNo, e.Sequence });

        modelBuilder.Entity<Hand>()
            .HasMany(h => h.DeckCards)
            .WithOne(dc => dc.Hand)
            .HasForeignKey(dc => new { dc.GameId, dc.HandNo })
            .IsRequired();

        modelBuilder.Entity<HandHoleCard>()
            .HasKey(e => new { e.GameId, e.HandNo, e.SeatNo, e.Sequence });

        modelBuilder.Entity<Hand>()
            .HasMany(h => h.HoleCards)
            .WithOne(pc => pc.Hand)
            .HasForeignKey(pc => new { pc.GameId, pc.HandNo })
            .IsRequired();

        modelBuilder.Entity<HandAction>()
            .HasKey(e => new { e.GameId, e.HandNo, e.No, e.SeatNo });

        modelBuilder.Entity<HandAction>()
            .Property(e => e.Street)
            .HasConversion<string>();

        modelBuilder.Entity<HandAction>()
            .Property(e => e.NextStreet)
            .HasConversion<string>();

        modelBuilder.Entity<HandAction>()
            .Property(e => e.PlayAction)
            .HasConversion<string>();
    }
}