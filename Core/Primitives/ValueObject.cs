namespace Core.Primitives;

/// <summary>Representa la clase base de la que derivan todos los objetos de valor.</summary>
/// <typeparam name="T">El tipo concreto del objeto de valor.</typeparam>
public abstract class ValueObject<T> : IEquatable<T> where T : ValueObject<T>
{
    public static bool operator ==(ValueObject<T>? a, ValueObject<T>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(ValueObject<T> a, ValueObject<T> b) => !(a == b);

    /// <inheritdoc />
    public bool Equals(T? other)
    {
        return other is not null &&
               GetType() == other.GetType() &&
               GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is T other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        foreach (var obj in GetAtomicValues())
            hashCode.Add(obj);
        return hashCode.ToHashCode();
    }

    /// <summary>Obtiene los valores atómicos del objeto de valor.</summary>
    /// <returns>La colección de valores para la comparación de igualdad.</returns>
    protected abstract IEnumerable<object> GetAtomicValues();
}
