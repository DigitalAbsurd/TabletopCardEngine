using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Converters;

namespace OpenHoldemEngine.Data;

public class HoldemDbContext : DbContext
{
    public HoldemDbContext(DbContextOptions dbContextOptions)
        : base(dbContextOptions) { }

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

        modelBuilder.Entity<HandDeckCard>()
            .Property(e => e.Card)
            .HasConversion<CardToStringConverter>()
            .HasMaxLength(2);

        modelBuilder.Entity<HandPlayerCard>()
            .Property(e => e.Card)
            .HasConversion<CardToStringConverter>()
            .HasMaxLength(2);

        modelBuilder.Entity<HandDeckCard>()
            .HasKey(e => new { e.GameId, e.HandNo, e.Sequence });

        modelBuilder.Entity<HandPlayerCard>()
        .HasKey(e => new { e.GameId, e.HandNo, e.SeatNo, e.Sequence });

        modelBuilder.Entity<HandAction>()
        .HasKey(e => new { e.GameId, e.HandNo, e.SeatNo, e.No });
    }
}