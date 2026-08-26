using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Converters;

namespace OpenHoldemEngine.Data;

internal class HoldemDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>()
            .HasMany(game => game.Hands)
            .WithOne(hand => hand.Game)
            .HasForeignKey(hand => hand.GameId);

        modelBuilder.Entity<Game>()
            .HasMany(game => game.BuyIns)
            .WithOne(buyIn => buyIn.Game)
            .HasForeignKey(buyIn => buyIn.GameId);

        modelBuilder.Entity<Hand>()
            .HasMany(hand => hand.Actions)
            .WithOne(action => action.Hand)
            .HasForeignKey(action => new { action.GameId, action.HandNo });

        modelBuilder.Entity<Player>()
            .HasKey(entity => new { entity.GameId, entity.SeatNo, entity.HandNoStart });

        modelBuilder.Entity<BuyIn>()
            .HasKey(entity => new { entity.GameId, entity.SeatNo, entity.HandNo });

        modelBuilder.Entity<Hand>()
            .HasKey(entity => new { entity.GameId, entity.No });

        modelBuilder.Entity<HandPlayerStartState>()
            .HasKey(entity => new { entity.GameId, entity.HandNo, entity.SeatNo });

        modelBuilder.Entity<HandPlayerEndState>()
            .HasKey(entity => new { entity.GameId, entity.HandNo, entity.SeatNo });

        modelBuilder.Entity<Hand>()
            .HasMany(hand => hand.PlayerStartStates)
            .WithOne(startState => startState.Hand)
            .HasForeignKey(startState => new { startState.GameId, startState.HandNo })
            .IsRequired();

        modelBuilder.Entity<Hand>()
            .HasMany(hand => hand.PlayerEndStates)
            .WithOne(endState => endState.Hand)
            .HasForeignKey(endState => new { endState.GameId, endState.HandNo })
            .IsRequired();

        modelBuilder.Entity<HandDeckCard>()
            .Property(entity => entity.Card)
            .HasConversion<CardToStringConverter>()
            .HasMaxLength(2);

        modelBuilder.Entity<HandHoleCard>()
            .Property(entity => entity.Card)
            .HasConversion<CardToStringConverter>()
            .HasMaxLength(2);

        modelBuilder.Entity<HandDeckCard>()
            .HasKey(entity => new { entity.GameId, entity.HandNo, entity.Sequence });

        modelBuilder.Entity<Hand>()
            .HasMany(hand => hand.DeckCards)
            .WithOne(deckCard => deckCard.Hand)
            .HasForeignKey(deckCard => new { deckCard.GameId, deckCard.HandNo })
            .IsRequired();

        modelBuilder.Entity<HandHoleCard>()
            .HasKey(entity => new { entity.GameId, entity.HandNo, entity.SeatNo, entity.Sequence });

        modelBuilder.Entity<Hand>()
            .HasMany(hand => hand.HoleCards)
            .WithOne(holeCard => holeCard.Hand)
            .HasForeignKey(holeCard => new { holeCard.GameId, holeCard.HandNo })
            .IsRequired();

        modelBuilder.Entity<HandAction>()
            .HasKey(entity => new { entity.GameId, entity.HandNo, entity.No, entity.SeatNo });

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.Street)
            .HasConversion<string>();

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.NextStreet)
            .HasConversion<string>();

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.ActionType)
            .HasConversion<string>();

        modelBuilder.Entity<HandPlayerEndState>()
            .Property(entity => entity.RankType)
            .HasConversion<string>();
    }
}
