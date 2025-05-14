# Guía de implementación de _Errores Custom Centralizados con Patrón Result y Estilo Funcional_
========================================================================

Esta guía recoge cómo integrar de forma práctica y progresiva el patrón `Result`, los errores de dominio centralizados y las extensiones funcionales en una aplicación moderna. Está pensada para ayudarte a aplicar estos conceptos de forma coherente en todo el flujo de la app (DataProvider → Servicio → PageModel → View), avanzar hacia un estilo funcional y mantener el código más limpio, predecible y testeable.

Una buena forma de empezar a incorporar este patrón es aplicarlo, especialmente al crear nuevas funcionalidades o flujos nuevos dentro de la aplicación si puede aportar valor.

---

## Contenido

1. [Comparativa: estilo tradicional vs funcional](#1-comparativa-estilo-tradicional-vs-funcional)
2. [Aplicación contextual de cada extensión](#2-aplicación-contextual-de-cada-extensión)
3. [Integración en el flujo completo entre capas](#3-integración-en-el-flujo-completo-entre-capas)
4. [Errores custom centralizados con Patrón Result y estilo funcional](#4-errores-custom-centralizados-con-patrón-result-y-estilo-funcional)

---

## 1. Comparativa: estilo tradicional vs funcional

---

La transición hacia un estilo funcional puede abordarse de forma progresiva. Para empezar, es útil visualizar las diferencias entre ambos enfoques en cuanto a estructura, control de errores y legibilidad.

### Estilo tradicional (imperativo)
```csharp
try
{
    var usuario = await servicio.ObtenerUsuarioPorId(id);
    if (!usuario.EsValido())
    {
        MostrarError("Usuario inválido");
        return;
    }

    _logger.LogInformation("Usuario correcto: {Email}", usuario.Email);
    Aplicar(usuario);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error inesperado en ObtenerUsuarioPorId");
    MostrarError("Error inesperado");
}
```

### Estilo funcional (declarativo)
```csharp
await servicio.ObtenerUsuarioPorId(id)
    .Ensure(u => u.EsValido(), ValidationError.InvalidUser)
    .Tap(u => _logger.LogInformation("Usuario correcto: {Email}", u.Email))
    .CatchAndLogError(_logger, GeneralError.Unexpected, nameof(servicio.ObtenerUsuarioPorId))
    .Match(
        onSuccess: Aplicar,
        onFailure: err => MostrarError(err.Message));
```

### Diferencias clave
| Aspecto                  | Estilo tradicional             | Estilo funcional                            |
|--------------------------|-------------------------------|---------------------------------------------|
| Control de errores       | try/catch                     | explícito con `Result` + `Match`            |
| Validaciones             | if / return early             | `Ensure`                                    |
| Side-effects             | inline                        | `Tap`, `TapError`                           |
| Logging de excepciones   | en catch                      | `CatchAndLogError`                          |
| Legibilidad              | bloques anidados              | flujo lineal y encadenado                   |
| Mantenibilidad           | lógica dispersa               | decisiones centralizadas                    |

> 💡 Cuanto más compleja sea una operación (más validaciones, logs, errores), más valor aporta este estilo.

---

## 2. Aplicación contextual de cada extensión

---

Elegir bien el momento y lugar para aplicar cada extensión de `Result` es clave para obtener un flujo declarativo, seguro y mantenible. Esta sección resume el propósito de cada extensión y el punto del flujo donde suele aportar mayor valor, aunque su uso puede variar dependiendo del diseño y la responsabilidad de cada capa.

> ℹ️ Aunque cada extensión tiene zonas donde aporta más valor, todas pueden usarse en cualquier capa si el contexto lo justifica. Algunos ejemplos:
> - `CatchAndLogError` suele usarse en capas bajas como `DataProvider`, pero también puede aplicarse en servicios si ahí se añade algún paso que pueda lanzar excepciones.
> - `TapError` es habitual en capas superiores, pero también puede ser útil en `DataProvider` si se necesita registrar un error funcional concreto justo en ese nivel.

| Extensión           | ¿Dónde suele aportar más valor?                           | ¿Para qué sirve?                                                       |
|---------------------|----------------------------------------------|------------------------------------------------------------------------|
| `CatchAndLogError` | En la **capa más baja** (DataProvider, adaptadores externos, llamadas HTTP, acceso a BD)       | Capturar y registrar excepciones inesperadas, convertirlas en errores |
| `Ensure`           | Justo después de una llamada exitosa         | Validar condiciones del resultado antes de continuar                  |
| `Tap`              | Después de un éxito (validado si aplica)     | Efectos secundarios (logs, métricas) sin alterar el resultado         |
| `Bind`             | En cualquier punto intermedio del flujo      | Encadenar operaciones que también devuelven `Result`                  |
| `Map`              | Antes de entregar el valor a capas superiores| Transformar el valor sin afectar al estado del `Result`               |
| `TapError`         | Cerca de la capa superior (servicio/UI)      | Registrar errores funcionales conocidos sin romper el flujo           |
| `Match`            | Como paso final                             | Separar y actuar según éxito o fallo                                  |

---

### Patrón típico recomendado
```csharp
await Operacion()
    .CatchAndLogError(...)   // errores inesperados (capa baja)
    .Ensure(...)             // validaciones de negocio
    .Tap(...)                // logs de éxito
    .Bind(...)               // llamada a otro paso dependiente
    .Map(...)                // transformar valor para devolverlo
    .TapError(...)           // log de error funcional si lo hay
    .Match(...)              // bifurcar lógica final
```

> 💡 Esta estructura permite aplicar validaciones, composición y logging en los puntos adecuados, sin mezclar responsabilidades entre capas, pero **no es necesario ni obligatorio el uso de todas las extensiones** de forma indiscriminada. 
Usa aquellas que tengan utilidad y aporten valor al flujo de código que estés creando.

---

## 3. Integración en el flujo completo entre capas

---

Uno de los mayores beneficios del patrón `Result` con extensiones funcionales es su capacidad para adaptarse a todas las capas de la aplicación. A continuación se muestra cómo se distribuyen las responsabilidades y se encadenan los flujos desde el acceso a datos hasta la vista.

---

### 📦 DataProvider
- Llama a APIs externas, BDs u otros recursos propensos a lanzar excepciones.
- Aplica `CatchAndLogError` para capturar errores inesperados y devolver un `Result.Failure` seguro.
- Puede aplicar `TapError` o `Ensure` si hay lógica de validación o trazabilidad específica.

```csharp
return await _apiClient.GetAsync<UserDto>(url)
    .CatchAndLogError(_logger, GeneralError.Unexpected, nameof(GetUserById), url);
```

---

### 🧠 Servicio de dominio
- Orquesta validaciones y llamadas a múltiples fuentes.
- Usa `Ensure`, `Bind`, `Tap`, `Map` y `TapError` según convenga.
- No suele repetir `CatchAndLogError` si ya se aplicó en DataProvider, salvo que añada pasos que puedan lanzar.

```csharp
return await _userProvider.GetById(userId)
    .Ensure(dto => !string.IsNullOrEmpty(dto.Email), ValidationError.Required("Email"))
    .Ensure(dto => IsValidEmailFormat(dto.Email), ValidationError.InvalidFormat("Email"))
    .Bind(dto => User.FromDto(dto))
    .Tap(user => _logger.LogInformation("Usuario cargado: {Email}", user.Email))
    .TapError(err => _logger.LogWarning("Error funcional en servicio: {Code}", err.Code));
```

---

### 🧩 PageModel / ViewModel
- Ejecuta la lógica de presentación sin ramificaciones manuales.
- Usa `Match` para bifurcar el flujo visual (éxito vs error).
- Opcionalmente, puede usar `Tap` para mostrar loaders, notificaciones o métricas.

```csharp
await _servicio.CargarUsuario(userId)
    .Tap(_ => MostrarSpinner(false))
    .Match(
        onSuccess: AplicarUsuario,
        onFailure: err => MostrarError(err.Message));
```

> ℹ️ Como comentamos en la [sección anterior](#2-aplicacion-contextual-de-cada-extension), aunque algunas extensiones suelen utilizarse en capas específicas por conveniencia (por ejemplo, `CatchAndLogError` en DataProvider o `TapError` en servicios), **no están limitadas a ellas**. Su aplicación dependerá del flujo real de código: si una operación lo requiere, puede ser totalmente válido y necesario usarlas en otras capas.

> 🎯 Este patrón permite que cada capa asuma una única responsabilidad, manteniendo la trazabilidad, limpieza y testabilidad del flujo completo.

---

## 4. Errores custom centralizados con Patrón Result y estilo funcional

--- 

Uno de los pilares de este enfoque es la combinación del patrón `Result` con errores de dominio centralizados. Esto permite mantener un control explícito y uniforme de los errores funcionales en todas las capas de la aplicación, facilitando la trazabilidad, el testing y la mantenibilidad.

---

### ¿Por qué centralizar los errores?

- **Evita duplicidades**: un mismo error (`ValidationError.InvalidInput`, `ValidationError.Required`, etc.) puede reutilizarse en diferentes partes del sistema.
- **Facilita el descubrimiento**: los desarrolladores pueden navegar fácilmente por los errores disponibles.
- **Permite internacionalización y personalización**: si se implementa correctamente, los errores pueden mapearse a mensajes localizables.
- **Refuerza la semántica del dominio**: los errores pasan a formar parte de la lógica explícita del negocio.

---

### Integración con Result

La clase `Error` hereda de `ValueObject<Error>`, lo que garantiza:

- Igualdad por valor (`Equals`, `==`, `GetHashCode`).
- Inmutabilidad.
- Consistencia en comparaciones y en estructuras como diccionarios o listas.

El patrón `Result` espera un `Error` en su forma fallida:

```csharp
return Result.Failure(ValidationError.Required("Email"));
```

Al integrar ambos patrones:

- Ganas consistencia tipada entre capas.
- Puedes usar expresiones como `result.Error == GeneralError.NotFound`.
- El control de errores deja de depender de strings sueltos o excepciones no tipadas.

---

### Uso funcional

Al combinar `Result`, extensiones y errores custom:

```csharp
await _userProvider.GetById(userId)
    .Ensure(dto => !string.IsNullOrEmpty(dto.Email), ValidationError.Required("Email"))
    .Ensure(dto => IsValidEmailFormat(dto.Email), ValidationError.InvalidFormat("Email"))
    .Map(dto => User.FromDto(dto))
    .Tap(user => _logger.LogInformation("Usuario cargado: {Email}", user.Email))
    .TapError(error => _logger.LogWarning("Error en User: {Codigo}", error.Code))
    .Match(
        onSuccess: AplicarUsuario,
        onFailure: error => MostrarError(error.Message));
```

En este ejemplo:
- El error funcional está claramente definido y centralizado.
- El control del flujo es declarativo y explícito.
- Se elimina por completo el uso de `try/catch` para flujos esperables.

> ✅ Esta integración es la que permite escribir código declarativo, seguro, trazable y preparado para ser probado fácilmente.

---

> **Documentación relacionada:**
> - [Guía de implementación de Errores y ValueObject](error-valueobject-guide.md)
> - [Guia de Result](result-guide.md)
> - [Guia de ResultExtensions](resultextensions-guide.md)
> - [Guia de FAQs](faqs.md)

[⬅️ Anterior: Guia de ResultExtensions](resultextensions-guide.md) | [Volver al inicio: Guia de Errores y ValueObject ⬆️](error-valueobject-guide.md)



