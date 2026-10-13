using FluentAssertions;
using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Data;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Guards that the model enforces precision/scale on decimals and max length on strings,
/// ensuring that per-provider migrations agree on column types.
/// </summary>
[TestClass]
public sealed class ModelConventionTests
{
    [TestMethod]
    public void Model_EveryDecimalHasPrecision_AndEveryStringHasMaxLength()
    {
        using var testData = TestEngineHost.Setup(out _, out var contextFactory);
        using var context = contextFactory.CreateDbContext();

        var offenders = new List<string>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var providerType = property.GetTypeMapping().Converter?.ProviderClrType ?? property.ClrType;
                var underlyingType = Nullable.GetUnderlyingType(providerType) ?? providerType;

                if (underlyingType == typeof(decimal))
                {
                    if (property.GetPrecision() is null || property.GetScale() is null)
                    {
                        offenders.Add($"{entityType.Name}.{property.Name}");
                    }
                }
                else if (underlyingType == typeof(string))
                {
                    if (property.GetMaxLength() is null)
                    {
                        offenders.Add($"{entityType.Name}.{property.Name}");
                    }
                }
            }
        }

        offenders.Should().BeEmpty("all decimals must have precision and scale, and all strings must have max length: {0}",
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void Model_PinsTheAgreedLengthsAndPrecision()
    {
        using var testData = TestEngineHost.Setup(out _, out var contextFactory);
        using var context = contextFactory.CreateDbContext();

        var playerNameProp = context.Model.FindEntityType(typeof(Player))?.FindProperty("Name");
        playerNameProp?.GetMaxLength().Should().Be(AmountRules.PlayerNameMaxLength, "Player.Name max length must match AmountRules");

        var cardProps = new[] {
            context.Model.FindEntityType(typeof(HandDeckCard))?.FindProperty("Card"),
            context.Model.FindEntityType(typeof(HandHoleCard))?.FindProperty("Card"),
        };
        foreach (var cardProp in cardProps.Where(p => p != null))
        {
            cardProp!.GetMaxLength().Should().Be(2, "Card columns must have max length 2");
        }

        var enumProps = new[] {
            context.Model.FindEntityType(typeof(HandAction))?.FindProperty("Street"),
            context.Model.FindEntityType(typeof(HandAction))?.FindProperty("NextStreet"),
            context.Model.FindEntityType(typeof(HandAction))?.FindProperty("ActionType"),
            context.Model.FindEntityType(typeof(Game))?.FindProperty("GameType"),
            context.Model.FindEntityType(typeof(Game))?.FindProperty("BettingStructure"),
            context.Model.FindEntityType(typeof(HandPlayerEndState))?.FindProperty("RankType"),
        };
        foreach (var enumProp in enumProps.Where(p => p != null))
        {
            enumProp!.GetMaxLength().Should().Be(32, "Enum string columns must have max length 32");
        }

        var descriptionProp = context.Model.FindEntityType(typeof(HandPlayerEndState))?.FindProperty("Description");
        descriptionProp?.GetMaxLength().Should().Be(32, "HandPlayerEndState.Description max length must be 32");

        var decimalProps = new[] {
            context.Model.FindEntityType(typeof(Game))?.FindProperty("InitialSmallBlind"),
            context.Model.FindEntityType(typeof(BuyIn))?.FindProperty("Amount"),
            context.Model.FindEntityType(typeof(HandAction))?.FindProperty("Size"),
            context.Model.FindEntityType(typeof(HandAction))?.FindProperty("Amount"),
            context.Model.FindEntityType(typeof(HandAction))?.FindProperty("AddAmount"),
            context.Model.FindEntityType(typeof(HandAction))?.FindProperty("Paid"),
            context.Model.FindEntityType(typeof(HandPlayerStartState))?.FindProperty("StartStack"),
            context.Model.FindEntityType(typeof(HandPlayerEndState))?.FindProperty("FinishStack"),
            context.Model.FindEntityType(typeof(HandPlayerEndState))?.FindProperty("WinAmount"),
        };
        foreach (var decimalProp in decimalProps.Where(p => p != null))
        {
            decimalProp!.GetPrecision().Should().Be(18, "Decimal columns must have precision 18");
            decimalProp!.GetScale().Should().Be(AmountRules.MaxDecimalPlaces, "Decimal columns must have scale equal to MaxDecimalPlaces");
        }
    }
}
