using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;

namespace Practicas_RepositorioRemoto.Validators;

/// <summary>
///     Contrato común de todos los validadores de dominio.
/// </summary>
/// <typeparam name="T">Tipo de entidad o DTO a validar.</typeparam>
public interface IValidador<T> {
    /// <summary>
    ///     Aplica todas las reglas de validación sobre el elemento recibido.
    /// </summary>
    /// <param name="elemento">Elemento a validar.</param>
    /// <returns>
    ///     <see cref="Result.Success{T, E}" /> si cumple todas las reglas;
    ///     <see cref="Result.Failure{T, E}" /> con un <see cref="Validation" />
    ///     que acumula todos los errores detectados, en caso contrario.
    /// </returns>
    Result<T, DomainError> Validar(T elemento);
}