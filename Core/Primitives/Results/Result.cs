namespace Core.Primitives.Results;

/// <summary>
///     Representa el resultado de una operación, con información de estado y posiblemente un error.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    ///     Obtiene un valor que indica si el resultado es exitoso.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Obtiene un valor que indica si el resultado es un fallo.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    ///     Obtiene el error.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    ///     Devuelve un <see cref="Result"/> exitoso.
    /// </summary>
    /// <returns>Una nueva instancia de <see cref="Result"/> con la bandera de éxito establecida.</returns>
    public static Result Success() => new Result(true, Error.None);

    /// <summary>
    ///     Devuelve un <see cref="Result{TValue}"/> exitoso con el valor especificado.
    /// </summary>
    /// <typeparam name="TValue">El tipo del resultado.</typeparam>
    /// <param name="value">El valor del resultado.</param>
    /// <returns>Una nueva instancia de <see cref="Result{TValue}"/> con la bandera de éxito establecida.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => new Result<TValue>(value, true, Error.None);

    /// <summary>
    ///     Crea un nuevo <see cref="Result{TValue}"/> con el valor nullable especificado y el error especificado.
    /// </summary>
    /// <typeparam name="TValue">El tipo del resultado.</typeparam>
    /// <param name="value">El valor del resultado.</param>
    /// <param name="error">El error en caso de que el valor sea null.</param>
    /// <returns>Una nueva instancia de <see cref="Result{TValue}"/> con el valor especificado o un error.</returns>
    public static Result<TValue> Create<TValue>(TValue? value, Error error)
        where TValue : class
        => value is null ? Failure<TValue>(error) : Success(value);

    /// <summary>
    ///     Devuelve un <see cref="Result"/> de fallo con el error especificado.
    /// </summary>
    /// <param name="error">El error.</param>
    /// <returns>Una nueva instancia de <see cref="Result"/> con el error especificado y la bandera de fallo establecida.</returns>
    public static Result Failure(Error error) => new Result(false, error);

    /// <summary>
    ///     Devuelve un <see cref="Result{TValue}"/> de fallo con el error especificado.
    /// </summary>
    /// <typeparam name="TValue">El tipo del resultado.</typeparam>
    /// <param name="error">El error.</param>
    /// <returns>Una nueva instancia de <see cref="Result{TValue}"/> con el error especificado y la bandera de fallo establecida.</returns>
    /// <remarks>
    /// Ignoramos intencionalmente la asignación nullable aquí porque la API nunca permitirá acceder a ella.
    /// El valor se accede a través de un método que lanzará una excepción si el resultado es un fallo.
    /// </remarks>
    public static Result<TValue> Failure<TValue>(Error error) => new Result<TValue>(default!, false, error);

    /// <summary>
    ///     Devuelve el primer fallo de los <paramref name="results"/> especificados.
    /// Si no hay fallo, se devuelve un éxito.
    /// </summary>
    /// <param name="results">El array de resultados.</param>
    /// <returns>
    /// El primer fallo del array de <paramref name="results"/> especificado, o un éxito si no existe.
    /// </returns>
    public static Result FirstFailureOrSuccess(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Success();
    }
}