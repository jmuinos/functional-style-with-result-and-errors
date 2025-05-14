# Guía de preguntas frecuentes sobre Result y errores funcionales
========================================================================

## 1. ❓ ¿Qué pasa si necesito encadenar varios DataProviders?
Puedes usar `Bind` para encadenar operaciones que devuelven `Result<T>`. Si una falla, se corta el flujo automáticamente.

```csharp
await GetUserByEmail(email)
    .Bind(user => GetUserPermissions(user.Id));
```

---

## 2. ❓ ¿Y si necesito lanzar varias operaciones en paralelo?
Cada operación puede ejecutarse con su propio `Result`. Puedes luego combinarlas manualmente, evaluando los fallos:

```csharp
var userResult = await GetUserById(id);
var rolesResult = await GetUserRoles(id);

var result = Result.FirstFailureOrSuccess(userResult, rolesResult);
```

También puedes combinar los datos si ambos son exitosos:
```csharp
if (userResult.IsSuccess && rolesResult.IsSuccess)
    return Result.Success(CombineUserAndRoles(userResult.Value, rolesResult.Value));
```

---

## 3. ❓ ¿Cómo debo gestionar el logging en cada capa?
La gestión de logs debe adaptarse al tipo de error y al nivel de la aplicación donde ocurre:

- Usa `CatchAndLogError` para capturar y registrar **excepciones inesperadas** en la capa más baja (como `DataProvider`, llamadas HTTP, acceso a BD). Esto actúa como una barrera de contención que evita que la excepción rompa el flujo.

- Si el servicio **añade lógica adicional que pueda fallar** (como validaciones, transformaciones o composición de resultados), entonces puede tener sentido aplicar un segundo `CatchAndLogError` o registrar con `TapError`.

- Usa `TapError` en capas superiores (servicio o presentación) para registrar **errores funcionales conocidos** con más contexto, como el identificador del usuario, el código de operación o el origen del fallo.

> ℹ️ Piensa en `CatchAndLogError` como una red de seguridad ante errores inesperados, y en `TapError` como una herramienta de trazabilidad para errores esperados que forman parte del flujo de negocio.

---

## 4. ❓ ¿Puedo acumular errores?
Este patrón está diseñado para propagación temprana (_short-circuit_), no acumulación. Para escenarios donde necesites recolectar múltiples errores (como validaciones masivas), puedes:

- Crear tu propio tipo como `ResultList<T>`.
- Devolver un `Result<List<Error>>` si los errores se recopilan manualmente.

---

## 5. ❓ ¿Puedo usar `async/await` dentro de extensiones como `Tap` o `Ensure`?
No directamente. Las extensiones como `Tap`, `Ensure`, `Map`, `Bind`, etc. están diseñadas para recibir funciones **síncronas**, por lo que no puedes utilizar `await` dentro del delegado que les pasas.

Por ejemplo, esto **no funcionará correctamente**:
```csharp
result.Tap(async user => {
    var permissions = await GetUserPermissions(user.Id); // ⚠ Incorrecto: no se espera una función async aquí
    ...
});
```

Sin embargo, eso no significa que no puedas encadenar lógica asincrónica. Para ello, puedes trabajar directamente sobre un `Task<Result<T>>` y utilizar las **sobrecargas asincrónicas** de estas extensiones (definidas en tu clase `ResultExtensions`). Estas sobrecargas permiten mantener el estilo funcional en flujos asincrónicos **sin necesidad de hacer `await` manual intermedio**.

Estas versiones **no permiten usar `await` dentro del delegado**, pero sí te permiten seguir componiendo fluidez en el flujo:

✅ Ejemplo correcto usando tus extensiones asincrónicas:
```csharp
await GetUserByEmail(email)
    .Ensure(user => user.IsValid(), UserErrors.InvalidData)
    .Bind(user => GetUserPermissions(user.Id)) // <- llamada asincrónica correctamente integrada
    .Tap(user => LogUserCheck(user))
    .TapError(error => _logger.LogWarning("Error loading permissions: {Code}", error.Code))
    .Match(
        onSuccess: ShowPermissions,
        onFailure: error => ShowError(error.Message));
```

> ℹ️ Estas extensiones asincrónicas existen para **evitar desempaquetar manualmente la tarea (`await`) antes de aplicar lógica funcional**.  
> Sin ellas, tendrías que hacer algo como esto:

```csharp
var result = await GetUserByEmail(email);
if (result.IsFailure || !result.Value.IsValid())
    return Result.Failure<User>(UserErrors.InvalidData);

return Result.Success(result.Value);
```

---
## 6. ❓ ¿Cuándo usar errores centralizados vs ad hoc?
```csharp
if (!validDate)
{
var error = new Error("Order.InvalidDate", $"Fecha {date} inválida para la orden {orderId}.");
return Result.Failure<OrderResult>(error);
}
```

---

## 7. ❓ ¿Cómo manejar múltiples errores?
```csharp
// Usando Ensure para validaciones múltiples
return Result.Success(input)
    .Ensure(v => v != null, ValidationError.NullInput)
    .Ensure(v => v.Length > 0, ValidationError.EmptyInput)
    .Ensure(v => v.Length <= 100, ValidationError.TooLong);
```
> 🔄 Las extensiones de Result como `Ensure` pueden consultarse en la [Guía de ResultExtensions](./resultextensions-guide.md)

---
> **Documentación relacionada:**
> - [Guía de implementación de Errores y ValueObject](./error-valueobject-guide.md)
> - [Guia de Result](./result-guide.md)
> - [Guia de ResultExtensions](./resultextensions-guide.md)
> - [Guia de Uso Combinado](./uso-combinado-y-funcional.md)

[Volver al inicio: Guia de Errores y ValueObject ⬆️](./error-valueobject-guide.md) 