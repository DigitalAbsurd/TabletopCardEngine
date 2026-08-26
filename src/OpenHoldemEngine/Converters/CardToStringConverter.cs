using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Converters;

/// <summary>The EF column conversion for <see cref="Card"/> — delegates to its own canonical string form.</summary>
internal sealed class CardToStringConverter() : ValueConverter<Card, string>(
    card => card.ToString(),
    strValue => Card.FromString(strValue));
