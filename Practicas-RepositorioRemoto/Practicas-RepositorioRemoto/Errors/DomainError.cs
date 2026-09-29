namespace Practicas_RepositorioRemoto.Errors;

/// <summary>
/// Tipo de errores de dominio
/// </summary>
public record DomainError {
    /// <summary>
    /// Error cuando no se encuentra
    /// </summary>
    public sealed record NotFound(string resource, int id) : DomainError;
    /// <summary>
    /// Error de validacion
    /// </summary>
    public sealed record ValidationError(string field, string errorMessage) : DomainError;
    /// <summary>
    ///  Error en la comunicacion con la api
    /// </summary>
    public sealed record ApiError(int statusCode, string errorMessage) : DomainError;
}