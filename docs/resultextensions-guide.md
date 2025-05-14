# Guia de implementación de ResultExtensions
========================================================================

Las extensiones funcionales ([`ResultExtensions`](../../Core/Primitives/Results/ResultExtensions.cs)) enriquecen el patrón `Result` con operadores declarativos, permitiendo un manejo claro, seguro y mantenible de flujos condicionales, efectos secundarios, validaciones y gestión de excepciones.

## Tabla de Contenidos

1. [Introducción a las extensiones funcionales](#introduccion-a-las-extensiones-funcionales)
2. [Descripción detallada de cada extension](#descripcion-detallada-de-cada-extension)
   - [Bind](#bind-encadenar-operaciones-que-devuelven-result)
   - [Tap](#tap-efectos-secundarios-en-caso-de-exito)
   - [TapError](#taperror-efectos-secundarios-en-caso-de-error)
   - [Ensure](#ensure-validacion-de-condiciones)
   - [Map](#map-transformacion-del-valor-en-exito)
   - [Match](#match-ejecucion-condicional-segun-el-resultado)
   - [CatchAndLogError](#catchandlogerror-captura-de-excepciones-y-logging)
   - [Ejemplo completo](#ejemplo-completo)
   - [Diferencias clave](#diferencias-clave-bind-vs-tap-y-catchandlogerror-vs-taperror)
   - [Consejos generales](#consejos-generales)
3. [Structured Logging con extensiones Result](#structured-logging-con-extensiones-result)

## Introduccion a las extensiones funcionales

Las extensiones funcionales sobre el patrón `Result` proporcionan métodos que facilitan encadenar, transformar, validar y ejecutar efectos secundarios sobre los resultados, permitiendo un estilo de programación más declarativo, limpio y seguro.

Estas extensiones están definidas en la clase estática [`ResultExtensions`](../../Core/Primitives/Results/ResultExtensions.cs) e incluyen operadores comunes en la programación funcional, como:

### Por que usarlas

- **Legibilidad mejorada**: Simplifican el flujo y lo vuelven más declarativo.
- **Gestión centralizada de errores**: Permiten manejar errores y excepciones en puntos específicos del flujo, sin fragmentar el código.
- **Efectos secundarios controlados**: Facilitan realizar acciones auxiliares (logging, métricas...) sin modificar el resultado original.
- **Seguridad y validación**: Permiten validar condiciones de manera clara, transformando resultados o interrumpiendo flujos según corresponda.
- **Flexibilidad en composición**: Posibilitan combinar fácilmente múltiples operaciones síncronas y asíncronas de manera robusta y comprensible.

## Descripcion detallada de cada extension

A continuación se describen los métodos principales de [`ResultExtensions`](../../Core/Primitives/Results/ResultExtensions.cs) con ejemplos prácticos y escenarios típicos de uso. Todas las extensiones cuentan con versiones síncronas y asíncronas.

### Bind: Encadenar operaciones que devuelven Result

> **Propósito**: Permite secuenciar múltiples operaciones que devuelven `Result` y facilita manejar de forma limpia el flujo de errores sin anidar condicionales.
>
> **Comportamiento**:
> - Ejecuta las operaciones en orden.
> - Si una operación devuelve un fallo, el flujo se interrumpe y devuelve inmediatamente el error, omitiendo el resto de operaciones encadenadas.
>
> **Escenario típico**:
> - Obtener un usuario por su ID y validar inmediatamente su acceso antes de continuar con otras operaciones.
>
**Ejemplo práctico**:

```csharp
await ObtenerUsuarioPorId(id)
    .Bind(usuario => ValidarAcceso(usuario));
```

### Tap: Efectos secundarios en caso de exito

> **Propósito**: Permite ejecutar efectos secundarios o acciones adicionales cuando la operación previa es exitosa, sin alterar el resultado original.
>
> **Comportamiento**:
> - Ejecuta la acción secundaria únicamente si el resultado anterior es exitoso.
> - No modifica ni interrumpe el resultado o flujo original.
>
> **Escenario típico**:
> - Realizar un log o registro de métricas cuando una operación crítica, como obtener un usuario, se completa exitosamente.
>
**Ejemplo práctico**:

```csharp
await ObtenerUsuarioPorId(id)
    .Tap(usuario => _logger.LogInformation($"Usuario encontrado: {usuario.Id}"));
```

### TapError: Efectos secundarios en caso de error

> **Propósito**: Permite ejecutar efectos secundarios o acciones adicionales cuando la operación previa falla, sin alterar el resultado original.
>
> **Comportamiento**:
> - Ejecuta la acción secundaria únicamente si el resultado anterior es fallido.
> - No modifica ni interrumpe el resultado o flujo original.
>
> **Escenario típico**:
> - Registrar logs de errores o enviar notificaciones cuando una operación crítica falla.
>
**Ejemplo práctico**:

```csharp
await ObtenerUsuarioPorId(id)
    .TapError(error => _logger.LogError($"Error al obtener usuario: {error.Message}"));
```

### Ensure: Validacion de condiciones

> **Propósito**: Permite verificar una condición específica sobre el resultado exitoso previo. Si la condición no se cumple, devuelve automáticamente un fallo con un error proporcionado.
>
> **Comportamiento**:
> - Evalúa una condición específica sobre el resultado.
> - Si la condición falla, transforma el resultado exitoso en un resultado fallido con un error específico.
> - No afecta al flujo si la condición es exitosa.
>
> **Escenario típico**:
> - Validar condiciones importantes sobre un resultado exitoso antes de continuar con otras operaciones críticas.
>
**Ejemplo práctico**:

```csharp
await ObtenerUsuarioPorId(id)
    .Ensure(usuario => usuario.IsActive, new Error("Usuario.Inactivo", "El usuario está inactivo."));
```

### Map: Transformacion del valor en exito

> **Propósito**: Permite transformar el valor contenido en un resultado exitoso, manteniendo la propagación automática de errores si la operación previa falla.
>
> **Comportamiento**:
> - Aplica una función de transformación únicamente si el resultado es exitoso.
> - Mantiene la propagación automática de fallos si el resultado original era fallido.
>
> **Escenario típico**:
> - Convertir un modelo de dominio (`Usuario`) en un DTO (`UsuarioDto`) antes de devolverlo o presentarlo en una vista.
>
**Ejemplo práctico**:

```csharp
await ObtenerUsuarioPorId(id)
    .Map(usuario => new UsuarioDto(usuario.Id, usuario.Nombre));
```

### Match: Ejecucion condicional segun el resultado

> **Propósito**: Permite realizar acciones diferenciadas según si el resultado fue exitoso o fallido, mejorando el control del flujo y claridad.
>
> **Comportamiento**:
> - Si el resultado es exitoso, ejecuta la función `onSuccess`.
> - Si el resultado es fallido, ejecuta la función `onFailure`.
> - Retorna el valor devuelto por la función ejecutada.
>
> **Escenario típico**:
> - Mostrar al usuario una vista de datos si la operación fue exitosa, o un mensaje de error si falló.
>
**Ejemplo práctico**:

```csharp
await ObtenerUsuarioPorId(id)
    .Match(
        onSuccess: usuario => Mostrar(usuario),
        onFailure: error => MostrarError(error.Message));
```

### CatchAndLogError: Captura de excepciones y logging

> **Propósito**: Permite capturar excepciones inesperadas durante la ejecución de una operación asíncrona, registrarlas y transformarlas en un `Result.Failure` con un error de fallback.
> 
> **Comportamiento**:
> - Intenta ejecutar la tarea original.
> - Si se lanza una excepción, registra el error con un `ILogger` y devuelve un `Result.Failure` con un error predeterminado.
> - Si no hay excepción, retorna el `Result` original (éxito o fallo funcional).
>
> **Escenario típico**:
> - Evitar que una excepción inesperada (por ejemplo, fallo de red, bug) detenga el flujo funcional, registrando el error para análisis posterior.
>
**Ejemplo práctico**:

```csharp
await ObtenerUsuarioPorId(id)
    .CatchAndLogError(
        _logger,
        new Error("General.Unexpected", "Error inesperado al obtener usuario"),
        nameof(ObtenerUsuarioPorId),
        id);
```

### Ejemplo completo

**Ejemplo práctico (MUY completo)**:

```csharp
await ObtenerUsuarioPorId(id)
    // Captura excepciones inesperadas y las transforma en un Failure con logging
    .CatchAndLogError(_logger, GeneralError.Unexpected, nameof(ObtenerUsuarioPorId), id)
    // Valida que el usuario esté activo; si no, interrumpe el flujo
    .Ensure(usuario => usuario.IsActive, new Error("Usuario.Inactivo", "El usuario no está activo."))
    // Solo se ejecuta si Ensure no ha fallado
    .Tap(usuario => _logger.LogInformation("Usuario cargado: {Id}", usuario.Id))
    // Transformación válida solo si el resultado sigue siendo exitoso
    .Map(usuario => new UsuarioDto(usuario.Id, usuario.Nombre))
    // Se ejecuta solo si el resultado es un Failure
    .TapError(error => _logger.LogWarning("Acceso denegado: {Codigo} - {Mensaje}", error.Code, error.Message))
    // Rama final que separa éxito de error
    .Match(
        onSuccess: dto => Mostrar(dto),
        onFailure: error => MostrarError(error.Message));
```

**Flujo**:
- `CatchAndLogError`: captura cualquier excepción inesperada de la llamada a datos y la convierte en un error funcional.
- `Tap`: registra que se ha recuperado un usuario exitosamente.
- `Ensure`: valida que el usuario esté activo; si no, transforma el resultado en error.
- `TapError`: registra advertencias funcionales si ocurre un error validado.
- `Map`: transforma el modelo de dominio en un DTO para la vista.
- `Match`: bifurca la lógica final entre éxito y error.

### Diferencias clave (Bind vs Tap) y (CatchAndLogError vs TapError)

- **`Bind` vs `Tap`**:
  - `Bind` encadena operaciones que devuelven un nuevo `Result`. Si hay un fallo, se detiene el flujo y se propaga el error.
  - `Tap` ejecuta una acción **solo como efecto secundario** cuando el resultado es exitoso, pero no altera el flujo ni el valor.

- **`CatchAndLogError` vs `TapError`**:
  - `CatchAndLogError` captura excepciones inesperadas en llamadas `async`, las registra y convierte en errores funcionales 
  (fallback). Es útil en la **capa más baja**, como acceso a datos.
  - `TapError` actúa sobre errores **funcionales conocidos**, sin modificar el `Result`. Es ideal en **capas superiores** (servicios, 
  UI) para registrar o mostrar mensajes.

## Structured Logging con extensiones Result

El patrón `Result` facilita aplicar logging estructurado directamente dentro del flujo funcional gracias a extensiones como `Tap`, `TapError` y `CatchAndLogError`.

- Permite insertar trazas sin romper el encadenamiento.
- Captura información clave sin duplicar lógica.
- Mejora el seguimiento de errores y operaciones en herramientas como Seq o Application Insights.

**Extensiones que lo permiten**:

- `Tap`: registra información cuando el resultado es exitoso.
- `TapError`: registra información cuando hay un error funcional.
- `CatchAndLogError`: registra excepciones inesperadas y las transforma en fallos controlados.

**Ejemplo combinado**:
```csharp
await ObtenerUsuarioPorId(id)
    // Captura excepciones inesperadas y las transforma en un Failure con logging
    .CatchAndLogError(_logger, GeneralError.Unexpected, nameof(ObtenerUsuarioPorId), id)
    // Interrumpe el flujo si el usuario no está activo
    .Ensure(u => u.IsActive, new Error("Usuario.Inactivo", "No activo"))
    // Solo se ejecuta si Ensure no ha fallado 
    .Tap(u => _logger.LogInformation("Usuario activo: {UserId}", u.Id)) 
    // Transformación válida solo en éxito
    .Map(u => new UsuarioDto(u.Id, u.Nombre)) 
    // Se ejecuta solo si el resultado es un Failure
    .TapError(err => _logger.LogWarning("Error funcional: {Code} - {Msg}", err.Code, err.Message));
```
---

### Consejos generales

- Usa `Bind` para continuar el flujo de operaciones.
- Usa `Ensure` antes de transformar o usar un valor.
- Usa `Tap` y `TapError` para registrar información sin alterar el flujo.
- Usa `CatchAndLogError` en el borde del sistema (DataProviders, adaptadores externos), donde pueden lanzarse excepciones inesperadas 
y necesitas evitar que rompan el flujo.
- Usa `Match` como punto final para ejecutar lógica en función del resultado.

> ⚠️ **¡Ojo!**  
> Recuerda que `Tap`, `Map` y `Ensure` **solo se ejecutan si el resultado es exitoso**.  
> Si el flujo ya está en error (por ejemplo, tras un `Ensure` fallido), estas extensiones no se ejecutarán.  
>  
> Usa `Match` cuando necesites lógica que se ejecute **en todos los casos**, tanto en éxito como en fallo, de forma clara y explícita.

---

> **Documentación relacionada:**
> - [Guía de implementación de Errores y ValueObject](./error-valueobject-guide.md)
> - [Guia de Result](./result-guide.md)
> - [Guia de Uso Combinado](./uso-combinado-y-funcional.md)
> - [Guia de FAQs](./faqs.md)

[⬅️ Anterior: Guia de Result](./result-guide.md) | [Siguiente: Guia de Uso Combinado ➡️](./uso-combinado-y-funcional.md)
