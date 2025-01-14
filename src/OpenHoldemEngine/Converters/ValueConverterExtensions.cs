using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OpenHoldemEngine.Converters;
internal static class ValueConverterExtensions
{
    internal static T ConvertTo<T>(this ValueConverter valueConverter, object value)
        => (T)valueConverter.ConvertToProvider(value)!;

    internal static T ConvertFrom<T>(this ValueConverter valueConverter, object value)
    => (T)valueConverter.ConvertFromProvider(value)!;
}
