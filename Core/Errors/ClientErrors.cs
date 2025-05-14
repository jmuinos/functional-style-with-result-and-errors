using Core.Primitives;

namespace Core.Errors;

/// <summary>
///     Contiene los errores relacionados con el cliente.
/// </summary>
public static class ClientErrors
{
    public static class ValidationError
    {
        public static Error Required(string field) =>
            new($"Validation.{field}.Required", $"{field} es requerido.");

        public static Error InvalidFormat(string field) =>
            new($"Validation.{field}.InvalidFormat", $"{field} tiene un formato inválido.");
    }

    public static class AuthError
    {
        public static Error InvalidCredentials =>
            new("Auth.InvalidCredentials", "Credenciales inválidas.");

        public static Error SessionExpired =>
            new("Auth.SessionExpired", "La sesión ha expirado.");
    }

    public static class GeneralError
    {
        public static Error Unexpected =>
            new("General.Unexpected", "Error inesperado.");

        public static Error Timeout =>
            new("General.Timeout", "La operación ha excedido el tiempo de espera.");
    }
}