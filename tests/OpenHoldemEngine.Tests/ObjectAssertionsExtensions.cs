using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace OpenHoldemEngine.Tests;

internal static class ObjectAssertionsExtensions
{
    private static readonly JsonSerializer _jsonSerializer;

    static ObjectAssertionsExtensions()
    {
        _jsonSerializer = JsonSerializer.CreateDefault();
        _jsonSerializer.Converters.Add(new StringEnumConverter());
    }

    internal static JTokenAssertions ShouldAsJToken(this object obj)
        => new(JToken.FromObject(obj, _jsonSerializer));

    internal static AndConstraint<JTokenAssertions> BeEquivalentToAsJToken(this JTokenAssertions assertions, object obj)
        => assertions.BeEquivalentTo(JToken.FromObject(obj, _jsonSerializer));
}
