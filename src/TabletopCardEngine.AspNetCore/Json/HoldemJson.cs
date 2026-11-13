using System.Text.Json.Serialization;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.AspNetCore.Json;

internal static class HoldemJson
{
    internal static void AddConverters(IList<JsonConverter> converters)
    {
        converters.Add(new CardJsonConverter());
        converters.Add(new JsonStringEnumConverter<AnteType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<BettingStructure>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<BlindIncreaseMode>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<CardSuit>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<CardType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<GameType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<HandActionType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<HandRankType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<HoldemTableFormat>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<PlayerActionType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<Street>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<TournamentEntryState>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<TournamentState>(namingPolicy: null, allowIntegerValues: false));
    }
}
