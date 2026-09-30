using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Converters;
using OpenHoldemEngine.HoldemActionHandlers;

namespace OpenHoldemEngine.Data;

internal class HoldemDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
{
    private const int EnumColumnMaxLength = 32;

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

        modelBuilder.Entity<Player>()
            .Property(entity => entity.Name)
            .HasMaxLength(AmountRules.PlayerNameMaxLength);

        modelBuilder.Entity<BuyIn>()
            .HasKey(entity => new { entity.GameId, entity.SeatNo, entity.HandNo });

        modelBuilder.Entity<BuyIn>()
            .Property(entity => entity.Amount)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<Hand>()
            .HasKey(entity => new { entity.GameId, entity.No });

        modelBuilder.Entity<HandPlayerStartState>()
            .HasKey(entity => new { entity.GameId, entity.HandNo, entity.SeatNo });

        modelBuilder.Entity<HandPlayerStartState>()
            .Property(entity => entity.StartStack)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<HandPlayerEndState>()
            .HasKey(entity => new { entity.GameId, entity.HandNo, entity.SeatNo });

        modelBuilder.Entity<HandPlayerEndState>()
            .Property(entity => entity.FinishStack)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<HandPlayerEndState>()
            .Property(entity => entity.WinAmount)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<HandPlayerEndState>()
            .Property(entity => entity.Description)
            .HasMaxLength(32);

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
            .Property(entity => entity.Size)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.Amount)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.AddAmount)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.Paid)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.Street)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.NextStreet)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<HandAction>()
            .Property(entity => entity.ActionType)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<Game>()
            .Property(entity => entity.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<Game>()
            .Property(entity => entity.InitialSmallBlind)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<Game>()
            .Property(entity => entity.GameType)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<Game>()
            .Property(entity => entity.BettingStructure)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<HandPlayerEndState>()
            .Property(entity => entity.RankType)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<OheSchemaRow>()
            .ToTable("__OheSchema");

        modelBuilder.Entity<OheSchemaRow>()
            .HasKey(entity => entity.Id);

        modelBuilder.Entity<OheSchemaRow>()
            .Property(entity => entity.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<OheSchemaRow>()
            .Property(entity => entity.SchemaVersion);

        modelBuilder.Entity<OheSchemaRow>()
            .Property(entity => entity.EngineVersion)
            .HasMaxLength(32);

        modelBuilder.Entity<OheSchemaRow>()
            .HasData(new OheSchemaRow { Id = 1, SchemaVersion = HoldemSchema.Version, EngineVersion = HoldemSchema.EngineVersion });
    }
}
