using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// One row of an object dump in the RoslynPad style: a name, a value and a type, plus the members
/// or items under it. Children are built only when first asked for, so a dump costs what is
/// actually expanded, not the size of the whole object graph.
/// </summary>
public sealed class ObjectDumpNode
{
    private readonly Lazy<IReadOnlyList<ObjectDumpNode>> _children;

    internal ObjectDumpNode(string name, string value, string typeName, Func<IReadOnlyList<ObjectDumpNode>> children)
    {
        Name = name;
        Value = value;
        TypeName = typeName;
        _children = new Lazy<IReadOnlyList<ObjectDumpNode>>(children);
    }

    /// <summary>The member name, or <c>[i]</c> for an item of a list.</summary>
    public string Name { get; }

    /// <summary>
    /// Always a value, never a type name: a scalar as written, an object or a list as a short
    /// one-line preview of what is inside it.
    /// </summary>
    public string Value { get; }

    /// <summary>The runtime type, <c>Name (Namespace)</c> as RoslynPad writes it.</summary>
    public string TypeName { get; }

    /// <summary>The members of an object, or the items of a list; empty for a scalar.</summary>
    public IReadOnlyList<ObjectDumpNode> Children => _children.Value;

    /// <summary>Whether there is anything to expand.</summary>
    public bool HasChildren => Children.Count > 0;
}

/// <summary>Builds <see cref="ObjectDumpNode"/> trees by reflection over public properties.</summary>
public static class ObjectDump
{
    /// <summary>How many levels of nesting a preview spells out before it writes <c>{…}</c> or <c>[…]</c>.</summary>
    public const int MaxPreviewDepth = 2;

    /// <summary>Previews longer than this are cut, with a trailing ellipsis; the tooltip is the same text.</summary>
    public const int MaxPreviewLength = 300;

    private const char Ellipsis = '…';

    /// <summary>Dumps <paramref name="value"/> as a root node called <paramref name="name"/>.</summary>
    /// <param name="name">The name shown for the root, e.g. <c>result</c>.</param>
    /// <param name="value">The object to dump; may be null.</param>
    /// <param name="declaredType">
    /// Shown as the type when <paramref name="value"/> is null, or when its runtime type is not public
    /// (the engine's collections are internal list types behind <c>IReadOnlyList&lt;T&gt;</c>).
    /// </param>
    /// <returns>The root node; its children are built on demand.</returns>
    public static ObjectDumpNode Create(string name, object? value, Type declaredType)
    {
        ArgumentNullException.ThrowIfNull(declaredType);

        var type = ShownType(value, declaredType);
        return new ObjectDumpNode(
            name,
            Truncate(Preview(value, depth: 0)),
            FormatTypeName(type),
            () => value is null || IsLeaf(type) ? [] : BuildChildren(value));
    }

    /// <summary>
    /// The runtime type when it is public; otherwise its nearest public base type, or the declared type
    /// when that base is only <see cref="object"/>. A non-public runtime type is an implementation
    /// detail (the engine's internal list, a compiler-synthesized collection) and naming it would leak it.
    /// </summary>
    private static Type ShownType(object? value, Type declaredType)
    {
        var type = value?.GetType();
        while (type is not null && !type.IsVisible)
        {
            type = type.BaseType;
        }

        return type is null || (type == typeof(object) && declaredType != typeof(object)) ? declaredType : type;
    }

    /// <summary>The type as RoslynPad shows it: <c>SeatData[] (OpenHoldemEngine.Dto)</c>, <c>List&lt;IError&gt; (System.Collections.Generic)</c>.</summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The friendly name, followed by the namespace in parentheses when there is one.</returns>
    public static string FormatTypeName(Type type)
    {
        var elementType = type;
        while (elementType.IsArray)
        {
            elementType = elementType.GetElementType()!;
        }

        var ns = elementType.Namespace;
        return string.IsNullOrEmpty(ns) ? FriendlyName(type) : $"{FriendlyName(type)} ({ns})";
    }

    private static string FriendlyName(Type type)
    {
        if (type.IsArray)
        {
            return FriendlyName(type.GetElementType()!) + "[]";
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return FriendlyName(underlying) + "?";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name;
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        if (tick >= 0)
        {
            name = name[..tick];
        }

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
    }

    private static List<ObjectDumpNode> BuildChildren(object value)
    {
        if (value is IEnumerable items)
        {
            return items.Cast<object?>()
                .Select((item, index) => Create($"[{index.ToString(CultureInfo.InvariantCulture)}]", item, typeof(object)))
                .ToList();
        }

        return ReadableProperties(value.GetType())
            .Select(property => TryGetValue(property, value, out var propertyValue, out var failure)
                ? Create(property.Name, propertyValue, property.PropertyType)
                : new ObjectDumpNode(property.Name, failure, FormatTypeName(property.PropertyType), () => []))
            .ToList();
    }

    /// <summary>Public instance properties without indexers, alphabetically, as RoslynPad lists them.</summary>
    private static IEnumerable<PropertyInfo> ReadableProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal);

    /// <summary>
    /// Reads a property, turning a throwing getter into a message rather than an exception — a
    /// failed <c>Result&lt;T&gt;</c> throws from <c>Value</c>, and a debugger shows that as a value too.
    /// </summary>
    private static bool TryGetValue(PropertyInfo property, object target, out object? value, out string failure)
    {
        try
        {
            value = property.GetValue(target);
            failure = string.Empty;
            return true;
        }
        catch (TargetInvocationException exception)
        {
            var inner = exception.InnerException ?? exception;
            value = null;
            failure = $"threw {inner.GetType().Name}: {inner.Message}";
            return false;
        }
    }

    private static bool IsLeaf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Card);
    }

    private static string FormatLeaf(object value)
        => value switch
        {
            string text => $"\"{text}\"",
            char character => $"'{character}'",
            bool flag => flag ? "true" : "false",
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    /// <summary>
    /// The one-line preview: <c>{ A = 1, B = "x" }</c> for an object, <c>[ah, kd]</c> for a list,
    /// nesting spelled out <see cref="MaxPreviewDepth"/> levels deep.
    /// </summary>
    private static string Preview(object? value, int depth)
    {
        if (value is null)
        {
            return "null";
        }

        var type = value.GetType();
        if (IsLeaf(type))
        {
            return FormatLeaf(value);
        }

        if (value is IEnumerable items)
        {
            return depth >= MaxPreviewDepth
                ? $"[{Ellipsis}]"
                : $"[{JoinBounded(items.Cast<object?>().Select(item => Preview(item, depth + 1)))}]";
        }

        if (depth >= MaxPreviewDepth)
        {
            return $"{{{Ellipsis}}}";
        }

        var members = ReadableProperties(type).Select(property => TryGetValue(property, value, out var propertyValue, out _)
            ? $"{property.Name} = {Preview(propertyValue, depth + 1)}"
            : $"{property.Name} = <threw>");
        var body = JoinBounded(members);
        return body.Length == 0 ? "{ }" : $"{{ {body} }}";
    }

    /// <summary>Joins parts with commas, stopping once the text is past the preview cap so a big list is not previewed in full.</summary>
    private static string JoinBounded(IEnumerable<string> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            if (builder.Length > MaxPreviewLength)
            {
                builder.Append(Ellipsis);
                break;
            }

            builder.Append(part);
        }

        return builder.ToString();
    }

    private static string Truncate(string text)
        => text.Length <= MaxPreviewLength ? text : text[..MaxPreviewLength] + Ellipsis;
}
