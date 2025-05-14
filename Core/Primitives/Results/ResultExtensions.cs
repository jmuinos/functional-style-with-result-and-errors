using Microsoft.Extensions.Logging;

namespace Core.Primitives.Results;

/// <summary>
///     Contiene métodos de extensión funcionales para el patrón <see cref="Result"/>.
/// </summary>
public static class ResultExtensions
{
    #region Core Operations (Map, Bind, Match)

    /// <summary>
    ///     Mapea el valor de un <see cref="Result{T}"/> exitoso a un nuevo valor usando la función especificada.
    ///     Si el resultado es un fallo, se propaga el mismo error.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func)
    {
        return result.IsSuccess
            ? Result.Success(func(result.Value))
            : Result.Failure<TOut>(result.Error);
    }
    
    /// <summary>
    ///     Mapea asíncronamente el valor de un <see cref="Task{Result{T}}"/> exitoso a un nuevo valor usando la función especificada.
    ///     Si el resultado es un fallo, se propaga el mismo error.
    /// </summary>
    public static async Task<Result<TOut>> Map<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> func)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? Result.Success(func(result.Value))
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    ///     Vincula un <see cref="Result{T}"/> exitoso a otro resultado aplicando la función dada.
    ///     Esto permite encadenar operaciones que devuelven tipos <see cref="Result{T}"/>.
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> func)
    {
        return result.IsSuccess ? func(result.Value) : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    ///     Vincula un <see cref="Result{T}"/> exitoso a un <see cref="Result"/> sin valor.
    /// </summary>
    public static Result Bind<TIn>(this Result<TIn> result, Func<TIn, Result> func)
    {
        return result.IsSuccess ? func(result.Value) : Result.Failure(result.Error);
    }

    /// <summary>
    ///     Vincula asíncronamente un <see cref="Result{T}"/> exitoso a otro <see cref="Task{Result{T}}"/>.
    /// </summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func)
    {
        return result.IsSuccess ? await func(result.Value) : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    ///     Vincula asíncronamente un <see cref="Result{T}"/> exitoso a un <see cref="Task{Result}"/>.
    /// </summary>
    public static async Task<Result> Bind<TIn>(this Result<TIn> result, Func<TIn, Task<Result>> func)
    {
        return result.IsSuccess ? await func(result.Value) : Result.Failure(result.Error);
    }

    /// <summary>
    ///     Vincula un <see cref="Task{Result{T}}"/> a otra función de resultado asíncrona.
    /// </summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> func)
    {
        var result = await resultTask;
        return result.IsSuccess ? await func(result.Value) : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    ///     Vincula un <see cref="Task{Result{T}}"/> a una función de <see cref="Task{Result}"/>.
    /// </summary>
    public static async Task<Result> Bind<TIn>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result>> func)
    {
        var result = await resultTask;
        return result.IsSuccess ? await func(result.Value) : Result.Failure(result.Error);
    }

    /// <summary>
    ///     Coincide con un <see cref="Result{T}"/> y ejecuta la función apropiada dependiendo de si es éxito o fallo.
    /// </summary>
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
    }

    /// <summary>
    ///     Coincide con un <see cref="Result"/>.
    /// </summary>
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result.Error);
    }

    /// <summary>
    ///     Coincide asíncronamente con un <see cref="Task{Result{T}}"/>.
    /// </summary>
    public static async Task<TOut> Match<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure);
    }

    /// <summary>
    ///     Coincide asíncronamente con un <see cref="Task{Result}"/>.
    /// </summary>
    public static async Task<TOut> Match<TOut>(this Task<Result> resultTask, Func<TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure);
    }

    #endregion

    #region Validation (Ensure)

    /// <summary>
    ///     Asegura que el predicado dado sea verdadero para el valor dentro de un <see cref="Result{T}"/> exitoso.
    ///     Si el predicado falla, devuelve un fallo con el error especificado.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
    {
        if (result.IsFailure) return result;
        return predicate(result.Value) ? result : Result.Failure<T>(error);
    }

    /// <summary>
    ///     Asegura asíncronamente que el predicado dado sea verdadero para el valor dentro de un <see cref="Task{Result{T}}"/> exitoso.
    ///     Si el predicado falla, devuelve un fallo con el error especificado.
    /// </summary>
    public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, bool> predicate, Error error)
    {
        var result = await resultTask;

        return result.IsSuccess && predicate(result.Value)
            ? result
            : Result.Failure<T>(result.Error == Error.None ? error : result.Error);
    }
    
    #endregion

    #region Side Effects (Tap, TapError)

    /// <summary>
    ///     Ejecuta una acción en el valor de un <see cref="Result{T}"/> exitoso. No altera el resultado.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    /// <summary>
    ///     Ejecuta asíncronamente una acción en el valor de un <see cref="Task{Result{T}}"/> exitoso. No altera el resultado.
    /// </summary>
    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Action<T> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    /// <summary>
    ///     Ejecuta una acción si el <see cref="Result{T}"/> es failure (error funcional), sin alterar el Result.
    /// </summary>
    public static Result<T> TapError<T>(this Result<T> result, Action<Error> onError)
    {
        if (result.IsFailure) 
            onError(result.Error);
        return result;
    }

    /// <summary>
    ///     Ejecuta una acción si el <see cref="Task{Result{T}}"/> acaba en failure (error funcional), sin alterar el Result.
    /// </summary>
    public static async Task<Result<T>> TapError<T>(this Task<Result<T>> resultTask, Action<Error> onError)
    {
        var result = await resultTask;
        if (result.IsFailure)
            onError(result.Error);
        return result;
    }

    #endregion

    #region Error Handling (CatchAndLog)

    /// <summary>
    ///     Captura excepciones inesperadas en un <see cref="Task{Result{T}}"/> y las convierte en un resultado de fallo, registrando el error.
    /// </summary>
    /// <typeparam name="T">El tipo de valor del resultado.</typeparam>
    /// <param name="task">La tarea a ejecutar y monitorear para excepciones.</param>
    /// <param name="logger">La instancia del logger.</param>
    /// <param name="fallbackError">El error a devolver si se captura una excepción.</param>
    /// <param name="context">Contexto opcional (método o identificador) para el registro.</param>
    /// <param name="param">Parámetro opcional para incluir en el mensaje de registro.</param>
    /// <returns>
    ///     El resultado de la tarea si es exitosa, o un resultado de fallo con el error de respaldo.
    /// </returns>
    public static async Task<Result<T>> CatchAndLogError<T>(
        this Task<Result<T>> task,
        ILogger logger,
        Error fallbackError,
        string? context = null,
        object? param = null)
    {
        try
        {
            return await task;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Context} - Excepción no controlada {Param}", context, param);
            return Result.Failure<T>(fallbackError);
        }
    }

    #endregion
}