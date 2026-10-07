using System.Collections;

namespace TabletopCardEngine.Mappers;

/// <summary>
/// The list behind every collection a public DTO record returns, so that the record's
/// compiler-generated <c>Equals</c> compares those collections element by element instead of by
/// reference. An array would make two <c>GameData</c> values rebuilt from the same database state
/// unequal, and would let a caller write into the engine's result through it.
/// </summary>
/// <typeparam name="T">The element type; its own <see cref="EqualityComparer{T}.Default"/> decides element equality.</typeparam>
internal sealed class EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
{
    private readonly T[] _items;

    /// <summary>Copies <paramref name="items"/>, so a later change to the source cannot reach the list.</summary>
    internal EquatableList(IEnumerable<T> items)
    {
        _items = [.. items];
    }

    /// <summary>An empty list, for a hand with nothing on the board or nobody on the clock.</summary>
    internal static EquatableList<T> Empty { get; } = new([]);

    /// <inheritdoc/>
    public int Count => _items.Length;

    /// <inheritdoc/>
    public T this[int index] => _items[index];

    /// <summary>Element-wise, in order; lists of different lengths are never equal.</summary>
    public bool Equals(EquatableList<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_items.Length != other._items.Length)
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < _items.Length; i++)
        {
            if (!comparer.Equals(_items[i], other._items[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as EquatableList<T>);

    /// <summary>Combines the element hashes in order, consistent with <see cref="Equals(EquatableList{T})"/>.</summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
