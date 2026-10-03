using System.Text.Json.Serialization;

namespace OpenHoldemEngine.AspNetCore.Json;

internal static class HoldemJson
{
    internal static void AddConverters(IList<JsonConverter> converters)
    {
        converters.Add(new CardJsonConverter());
        converters.Add(new JsonStringEnumConverter<BettingStructure>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<CardSuit>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<CardType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<GameType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<HandActionType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<HandRankType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<PlayerActionType>(namingPolicy: null, allowIntegerValues: false));
        converters.Add(new JsonStringEnumConverter<Street>(namingPolicy: null, allowIntegerValues: false));
    }
}
