using System.Text.Json;
using System.Text.Json.Serialization;

namespace TabletopCardEngine.AspNetCore.Json;

internal sealed class CardJsonConverter : JsonConverter<Card>
{
    public override Card Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a card such as \"ah\".");
        }

        var text = reader.GetString();
        if (text is null || !Card.TryParse(text, out var card))
        {
            throw new JsonException("Expected a card such as \"ah\".");
        }

        return card;
    }

    public override void Write(Utf8JsonWriter writer, Card value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
