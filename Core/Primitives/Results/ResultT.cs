namespace Core.Primitives.Results;

/// <summary>
///     Representa el resultado de una operación, con información de estado y posiblemente un valor y un error.
/// </summary>
/// <typeparam name="TValue">El tipo del valor del resultado.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue _value;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="Result{TValueType}"/> con los parámetros especificados.
    /// </summary>
    /// <param name="value">El valor del resultado.</param>
    /// <param name="isSuccess">La bandera que indica si el resultado es exitoso.</param>
    /// <param name="error">El error.</param>
    protected internal Result(TValue value, bool isSuccess, Error error)
        : base(isSuccess, error)
        => _value = value;

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>
    ///     Obtiene el valor del resultado si es exitoso, de lo contrario lanza una excepción.
    /// </summary>
    /// <returns>El valor del resultado si es exitoso.</returns>
    /// <exception cref="InvalidOperationException"> cuando <see cref="Result.IsFailure"/> es true.</exception>
    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");
}