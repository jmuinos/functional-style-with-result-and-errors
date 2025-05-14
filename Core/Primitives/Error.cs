namespace Core.Primitives;

public sealed class Error(string code, string message) : ValueObject<Error>
{
    public string Code { get; } = code;
    public string Message { get; } = message;

    /// <summary>
    ///     Obtiene la instancia de error vacía (singleton).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    public static implicit operator string(Error error)
    {
        return error?.Code ?? string.Empty;
    }

    /// <inheritdoc />
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Code;
        yield return Message;
    }
}