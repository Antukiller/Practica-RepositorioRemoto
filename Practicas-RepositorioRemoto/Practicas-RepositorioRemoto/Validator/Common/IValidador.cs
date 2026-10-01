using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;

namespace Practicas_RepositorioRemoto.Validator.Common;

// ─── VALIDATION CONTRACT ───

/// <summary>
/// Contrato para validar entidades del dominio
/// </summary>
/// <typeparam name="T">Tipo de la entidad a validar</typeparam>
public interface IValidador<T> {
    /// <summary>
    /// Valida una entidad según las reglas de dominio.
    /// </summary>
    /// <param name="entidad">Entidad a validar</param>
    /// <returns>Result con la entidad validada o error si no es válido</returns>
    Result<T, DomainError> Validar(T entidad);
}