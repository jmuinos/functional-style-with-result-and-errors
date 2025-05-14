# Guía de implementación de Error & ValueObject
========================================================================

## 📋 Tabla de Contenidos

- [Introducción](#introduccion)
- [Requisitos Previos](#requisitos-previos)
- [Conceptos Básicos](#conceptos-basicos)
   - [ValueObject<T>](#1-valueobjectt-igualdad-por-valor)
   - [Error](#2-error-value-object-para-errores)
- [Implementación](#implementacion)
  - [ClientErrors](#3-client-errors-centralizacion-de-codigos)
  - [Errores Ad Hoc](#4-creacion-de-errores-ad-hoc)
- [Casos de Uso](#casos-de-uso)
- [Beneficios de la Implementación](#beneficios-de-la-implementacion)

## Introduccion

Organizar y modelar errores de dominio de forma consistente es clave para mantener código limpio, seguro y fácil de mantener. Esta guía presenta una implementación robusta de errores basada en Value Objects y patrones funcionales.

## Requisitos Previos

- Conocimiento básico de C#
- Familiaridad con programación orientada a objetos
- Comprensión básica de programación funcional (opcional pero recomendado)

## Conceptos Basicos

### 1. `ValueObject`: Igualdad por valor

#### Definición
La clase base [`ValueObject`](../Core/Primitives/ValueObject.cs) proporciona una implementación genérica para objetos de valor con igualdad por valor.

#### ¿Por qué usarlo?

* **Elimina boilerplate**: No necesitas implementar `Equals`/`GetHashCode` en cada clase.
* **Comparaciones consistentes**: Garantiza que dos objetos con el mismo contenido son iguales.
* **Inmutabilidad**: Fomenta el uso de objetos inmutables.
* **Testing**: Facilita las pruebas unitarias y el uso en colecciones.

### 2. `Error`: Value object para errores

#### Definición
La clase [`Error`](../Core/Primitives/Error.cs) implementa [`ValueObject<Error>`](../Core/Primitives/ValueObject.cs) para representar errores de dominio de forma inmutable y comparable.

#### Ventajas

* **Inmutabilidad**: Los errores no pueden modificarse después de su creación.
* **Comparabilidad**: Dos errores con el mismo código y mensaje son iguales.
* **Singleton `None`**: un único objeto que representa "sin error".  
  > - **¿Por qué?** Evita el uso de `null` para indicar ausencia de fallo, lo que reduce el riesgo de `NullReferenceException` y 
  simplifica el código: siempre puedes devolver o comparar contra `Error.None` en lugar de hacer `if (error != null)`.  
  > - **Comparaciones fiables**: como `Error.None` es una instancia única, cualquier comparación de referencia (`err == Error.None`) 
  funciona de forma predecible.
* **Conversión implícita**: Facilita el logging y la serialización.

#### Ejemplo de uso
```csharp
// Crear un error
var validationError = new Error("ValidationError.InvalidInput", "El input no es válido");

// Comparar errores
var validationError1 = new Error("ValidationError.InvalidInput", "El input no es válido");
var validationError2 = new Error("ValidationError.InvalidInput", "El input no es válido");
validationError1 == validationError2; // true

// Usar Error.None
Error noError = Error.None;
if (noError == Error.None)
{
    // No hay error
}
```

## Implementacion

### 3. `ClientErrors`: Centralizacion de Codigos

#### Definicion
La clase [`ClientErrors`](../Core/Errors/ClientErrors.cs) centraliza los códigos de error reutilizables y organizados por contexto.

#### Convención de nombres

* **`ClientErrors`**: Contenedor principal.
* **`{Contexto}Error`**: Subclase para cada contexto específico.
* **Propiedades estáticas**: Devuelven instancias de `Error`.

#### Ejemplo de uso
```csharp
// En un validador
if (string.IsNullOrEmpty(email))
{
    return Result.Failure(ValidationError.Required("Email"));
}

// En un servicio de autenticación
if (!IsValidCredentials(username, password))
{
    return Result.Failure(AuthError.InvalidCredentials);
}
```

### 4. Creacion de errores Ad Hoc

#### Cuando usarlos

* Mensajes específicos con parámetros dinámicos.
* Errores que no se reutilizarán.
* Casos especiales que requieren contexto adicional.

#### Ejemplo
```csharp
// Error con parámetros dinámicos
var error = new Error(
    "Order.InvalidDate",
    $"La fecha {orderDate} no es válida para la orden {orderId}."
);

// Error con contexto específico
var error = new Error(
    "Payment.Declined",
    $"El pago fue rechazado por el banco. Razón: {declineReason}"
);
```

## Casos de Uso

### Validacion de Formularios
```csharp
public Result<User> ValidateUser(User user)
{
    return Result.Success(user)
        .Ensure(u => !string.IsNullOrEmpty(u.Email), 
            ClientErrors.ValidationError.Required("Email"))
        .Ensure(u => IsValidEmailFormat(u.Email),
            ClientErrors.ValidationError.InvalidFormat("Email"))
        .Ensure(u => u.Age >= 18,
            new Error("Validation.Age.Underage", "Debes ser mayor de edad"));
}
```

### Manejo de Errores en APIs
```csharp
public async Task<Result<Order>> ProcessOrder(OrderRequest request)
{
    try
    {
        var order = await _orderService.CreateOrder(request);
        return Result.Success(order);
    }
    catch (TimeoutException)
    {
        return Result.Failure<Order>(GeneralError.Timeout);
    }
    catch (Exception)
    {
        return Result.Failure<Order>(GeneralError.Unexpected);
    }
}
```

- Usa errores centralizados para casos comunes y reutilizables
- Usa errores ad hoc para casos específicos con parámetros dinámicos

## Beneficios de la Implementacion

### 1. Seguridad y Robustez
* **Inmutabilidad**: Los errores no pueden modificarse después de su creación
* **Null Safety**: `Error.None` elimina la necesidad de manejar `null`
* **Type Safety**: El sistema de tipos ayuda a prevenir errores en tiempo de compilación

### 2. Mantenibilidad
* **Centralización**: Todos los errores están en un solo lugar
* **Consistencia**: Convención uniforme en toda la aplicación
* **Discoverability**: Autocompletado y documentación integrada

### 3. Testing
* **Comparabilidad**: Fácil de comparar y verificar en pruebas
* **Predictibilidad**: Comportamiento consistente y predecible
* **Aislamiento**: Errores independientes y autocontenidos

### 4. Integracion
* **Logging**: Fácil integración con sistemas de logging
* **Serialización**: Conversión simple a JSON/string
* **APIs**: Formato consistente para APIs y servicios

### 5. Desarrollo
* **Productividad**: Menos código boilerplate
* **Claridad**: Intención explícita en el código
* **Reutilización**: Patrones y errores reutilizables

---

> **Documentación relacionada:**
> - [Guia de Result](result-guide.md)
> - [Guia de ResultExtensions](resultextensions-guide.md)
> - [Guia de Uso Combinado](uso-combinado-y-funcional.md)
> - [Guia de FAQs](faqs.md)

[Siguiente: Guia de Result ➡️](result-guide.md)