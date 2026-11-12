using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TabletopCardEngine.Converters;
using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.Data;

internal class HoldemDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
{
    private const int EnumColumnMaxLength = 32;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.ProviderName is HoldemProviders.PostgreSql or HoldemProviders.SqlServer)
        {
            modelBuilder.HasDefaultSchema(HoldemSchema.DatabaseSchema);
        }

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

        // HandAction reaches Game twice: directly, and through Hand, which cascades from Game. SQL Server
        // rejects two cascade paths to one table, so there the direct one does not cascade, and deleting a
        // game removes its actions through Game -> Hand -> HandAction. It stays a cascade elsewhere, because
        // PostgreSQL checks a NO ACTION key on Game before the cascade through Hand has removed the actions.
        modelBuilder.Entity<HandAction>()
            .HasOne(action => action.Game)
            .WithMany()
            .HasForeignKey(action => action.GameId)
            .OnDelete(Database.ProviderName is HoldemProviders.SqlServer ? DeleteBehavior.NoAction : DeleteBehavior.Cascade);

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

        // The default is what rows stored before the column existed are: every one was a join.
        modelBuilder.Entity<BuyIn>()
            .Property(entity => entity.Kind)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength)
            .HasDefaultValue(BuyInKind.Join);

        modelBuilder.Entity<Game>()
            .Property(entity => entity.MinBuyIn)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<Game>()
            .Property(entity => entity.MaxBuyIn)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        // One JSON column: NULL is "no schedule". System.Text.Json writes a decimal exactly, so it round-trips.
        // The comparer compares the JSON, since the lists inside the record compare by reference.
        modelBuilder.Entity<Game>()
            .Property(entity => entity.BlindSchedule)
            .HasMaxLength(BlindScheduleJson.MaxLength)
            .HasConversion(
                schedule => BlindScheduleJson.Serialize(schedule),
                json => BlindScheduleJson.Deserialize(json),
                new ValueComparer<BlindScheduleData?>(
                    (left, right) => BlindScheduleJson.Serialize(left) == BlindScheduleJson.Serialize(right),
                    schedule => BlindScheduleJson.Serialize(schedule)!.GetHashCode(StringComparison.Ordinal),
                    schedule => BlindScheduleJson.Deserialize(BlindScheduleJson.Serialize(schedule))));

        modelBuilder.Entity<Hand>()
            .Property(entity => entity.SmallBlind)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<Hand>()
            .Property(entity => entity.Ante)
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

        modelBuilder.Entity<Game>()
            .Property(entity => entity.Ante)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        // The default is what games created before the column existed get: none has an ante, so the type is moot.
        modelBuilder.Entity<Game>()
            .Property(entity => entity.AnteType)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength)
            .HasDefaultValue(AnteType.Classic);

        // The default is what games created before the column existed get: they all played 2 to 6 seats.
        modelBuilder.Entity<Game>()
            .Property(entity => entity.TableFormat)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength)
            .HasDefaultValue(HoldemTableFormat.SixMax);

        modelBuilder.Entity<HandPlayerEndState>()
            .Property(entity => entity.RankType)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<Tournament>()
            .HasMany(tournament => tournament.Entries)
            .WithOne(entry => entry.Tournament)
            .HasForeignKey(entry => entry.TournamentId);

        modelBuilder.Entity<Tournament>()
            .HasMany(tournament => tournament.Tables)
            .WithOne(table => table.Tournament)
            .HasForeignKey(table => table.TournamentId);

        // The link from a game to its tournament lives here, not on Game. It does not cascade: the tournament
        // does not own the game, and SQL Server would not allow a second cascade path anyway.
        modelBuilder.Entity<TournamentTable>()
            .HasOne<Game>()
            .WithMany()
            .HasForeignKey(table => table.GameId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TournamentTable>()
            .HasIndex(table => table.GameId)
            .IsUnique();

        modelBuilder.Entity<TournamentEntry>()
            .HasKey(entity => new { entity.TournamentId, entity.EntryNo });

        modelBuilder.Entity<TournamentEntry>()
            .Property(entity => entity.Name)
            .HasMaxLength(AmountRules.PlayerNameMaxLength);

        modelBuilder.Entity<TournamentEntry>()
            .Property(entity => entity.CarriedStack)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<TournamentEntry>()
            .Property(entity => entity.Prize)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces)
            .HasDefaultValue(0M);

        modelBuilder.Entity<TournamentTable>()
            .HasKey(entity => new { entity.TournamentId, entity.TableNo });

        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.BettingStructure)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.TableFormat)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.AnteType)
            .HasConversion<string>()
            .HasMaxLength(EnumColumnMaxLength);

        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.StartingStack)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.BuyIn)
            .HasPrecision(18, AmountRules.MaxDecimalPlaces);

        // The two lists are JSON columns, compared by their JSON since a list compares by reference.
        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.Levels)
            .HasMaxLength(TournamentJson.MaxLength)
            .HasConversion(
                levels => TournamentJson.Serialize(levels),
                json => TournamentJson.Deserialize<TournamentLevel>(json),
                new ValueComparer<List<TournamentLevel>>(
                    (left, right) => TournamentJson.Serialize(left) == TournamentJson.Serialize(right),
                    levels => TournamentJson.Serialize(levels).GetHashCode(StringComparison.Ordinal),
                    levels => TournamentJson.Deserialize<TournamentLevel>(TournamentJson.Serialize(levels))));

        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.Payouts)
            .HasMaxLength(TournamentJson.MaxLength)
            .HasConversion(
                payouts => TournamentJson.Serialize(payouts),
                json => TournamentJson.Deserialize<decimal>(json),
                new ValueComparer<List<decimal>>(
                    (left, right) => TournamentJson.Serialize(left) == TournamentJson.Serialize(right),
                    payouts => TournamentJson.Serialize(payouts).GetHashCode(StringComparison.Ordinal),
                    payouts => TournamentJson.Deserialize<decimal>(TournamentJson.Serialize(payouts))));

        modelBuilder.Entity<Tournament>()
            .Property(entity => entity.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<TceSchemaRow>()
            .ToTable(HoldemSchema.SchemaTable);

        modelBuilder.Entity<TceSchemaRow>()
            .HasKey(entity => entity.Id);

        modelBuilder.Entity<TceSchemaRow>()
            .Property(entity => entity.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<TceSchemaRow>()
            .Property(entity => entity.SchemaVersion);

        modelBuilder.Entity<TceSchemaRow>()
            .Property(entity => entity.EngineVersion)
            .HasMaxLength(32);

        modelBuilder.Entity<TceSchemaRow>()
            .HasData(new TceSchemaRow { Id = 1, SchemaVersion = HoldemSchema.Version, EngineVersion = HoldemSchema.EngineVersion });

        // SQLite has no schemas, and HasDefaultSchema there is ignored with a warning, so Hold'em's
        // tables are kept apart from a host's and another game's by their names instead.
        if (Database.ProviderName is HoldemProviders.Sqlite)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(HoldemSchema.SqliteTablePrefix + entityType.GetTableName());
            }
        }
    }
}
