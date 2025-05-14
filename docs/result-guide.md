# Guía de implementación de Result & Result<T>
========================================================================

## 📋 Tabla de Contenidos

- [Introducción](#introduccion)
- [Requisitos Previos](#requisitos-previos)
- [Conceptos Básicos](#conceptos-basicos)
   - [Result y Result<T>](#result-y-resultt-contenedores-de-failure-o-success)
   - [Ventajas](#2-ventajas-frente-a-excepciones-tradicionales)
- [Implementación](#implementacion)
   - [Aplicación del Patron](#aplicacion-agnostica-del-patron)
   - [Estilos de Programación](#estilo-imperativo-vs-funcional)

## Introduccion

Modelar el resultado de operaciones de forma segura y expresiva es clave para construir aplicaciones robustas. Esta guía presenta el patrón `Result`, que encapsula el éxito o fallo de una operación sin necesidad de excepciones, y su variante genérica `Result<T>`, que transporta un valor en caso de éxito.

## Conceptos Basicos

### 1.`Result` y `Result<T>`: Contenedores de failure o success

#### Definicion

Las clases [`Result`](../../Core/Primitives/Results/Result.cs) y [`Result<T>`](../../Core/Primitives/Results/ResultT.cs) implementan el patrón de contenedor de éxito o fallo para operaciones, evitando el uso de excepciones para el control de flujo.

#### ¿Por qué usarlos?

* **Elimina nulos y excepciones como flujo normal**
  _Las operaciones devuelven explícitamente un éxito o un error, evitando `null`, `try/catch`, y mejorando la predictibilidad._

* **Proporciona un contenedor seguro**
  _Solo accedes al valor si `IsSuccess == true`. Si no, el acceso lanza excepción._

* **Facilita composición funcional**
  _Es el punto de partida para aplicar operadores como `Map`, `Bind`, `Ensure` y más, que trataremos en profundidad en la  [Guía de ResultExtensions](./resultextensions.md)._

* **Control total del flujo**
  _Cada llamada devuelve un `Result` y puede evaluarse con claridad, permitiendo decisiones seguras en cada capa._

#### Ejemplos de uso

```csharp
// ✅ Éxito sin valor
return Result.Success();

// ✅ Éxito con valor
return Result.Success(usuario);

// ❌ Fallo con error funcional
return Result.Failure(GeneralError.Unexpected);

// ❌ Fallo con error específico dinámico
return Result.Failure(new Error("Login.Failed", "Contraseña incorrecta"));
```

> ℹ️ Nota importante: Aunque `Result.Success(...)` y `Result.Failure(...)` se parezcan a `IsSuccess` y `IsFailure`, **no son lo mismo**. Los primeros son métodos estáticos usados para *crear* un `Result`; los segundos son propiedades que se utilizan para *consultar* el estado de una instancia ya creada.
> 
> Este matiz se explora con mayor profundidad en las siguientes guías:
> 
> * [Guía de ResultExtensions](./resultextensions.md)
> * [Guía de implementación de _Errores Custom Centralizados y Estilo Funcional_](./uso-combinado-y-funcional.md)

### 2. Ventajas frente a excepciones tradicionales

| Característica        | Enfoque tradicional  | Con `Result`                        |
| --------------------- | -------------------- | ----------------------------------- |
| Control de errores    | try/catch            | If explícito con `IsFailure`        |
| Comunicación de fallo | Excepciones o null   | `Error` con código y mensaje        |
| Pruebas unitarias     | Lanzar excepciones   | Comparar resultados y errores       |
| Legibilidad           | Flujo fragmentado    | Flujo fluido y declarativo          |
| Logging centralizado  | Manual por excepción | Con `TapError` o `CatchAndLogError` |

> 🧠 Las excepciones siguen siendo útiles, pero deberían reservarse para casos verdaderamente excepcionales e inesperados, como errores de infraestructura o bugs de lógica.

## Implementacion

### Aplicacion agnostica del Patron

Este patrón puede usarse en cualquier operación que pueda fallar, sin importar la el tipo de fuente de datos:

* **Bases de datos**

  ```csharp
  Result<Usuario> usuario = await repo.ObtenerPorId(id);
  ```

* **Llamadas HTTP**

  ```csharp
  Result<Configuracion> config = await client.GetSettings(codigo);
  ```

* **Lectura de ficheros**

  ```csharp
  Result<string> contenido = LeerArchivo(path);
  ```

* **Operaciones de negocio**

  ```csharp
  Result resultado = servicio.ValidarAcceso(usuario);
  ```

* **Validaciones**

  ```csharp
  return Result.Create(input, ClientErrors.Inputs.Empty);
  ```

### Estilo imperativo vs funcional

#### ❌ Estilo tradicional (imperativo)

```csharp
var resultado = await servicio.Validar();

if (!resultado.IsSuccess)
{
    _logger.LogError("Error: {ErrorCode}", resultado.Error.Code);
    return;
}

var siguiente = await servicio.SiguientePaso(resultado.Value);
if (!siguiente.IsSuccess)
{
    _logger.LogError("Error: {ErrorCode}", siguiente.Error.Code);
    return;
}
```

#### ✅ Estilo funcional

```csharp
await Validar()
    .CatchAndLogError(_logger, GeneralError.Unexpected, nameof(Validar))
    .Bind(v => SiguientePaso(v))
    .Tap(_ => _logger.LogInformation("Éxito"))
    .TapError(err => _logger.LogError("Error funcional: {0}", err.Code))
    .Match(
        onSuccess: Mostrar,
        onFailure: err => MostrarError(err.Message)
    );
```
> 🔄 Este enfoque funcional, en combinación con la gestión de errores centralizada, se explora más a fondo en la: [Guía de implementación de _Errores Custom Centralizados con Patrón Result y Estilo Funcional_](./uso-combinado-y-funcional.md)

---

> **Documentación relacionada:**
> - [Guía de implementación de Errores y ValueObject](./error-valueobject-guide.md)
> - [Guia de ResultExtensions](./resultextensions-guide.md)
> - [Guia de Uso Combinado](./uso-combinado-y-funcional.md)
> - [Guia de FAQs](./faqs.md)

[⬅️ Anterior: Guia de Errores y ValueObject](./error-valueobject-guide.md) | [Siguiente: Guia de ResultExtensions ➡️](./resultextensions-guide.md)
