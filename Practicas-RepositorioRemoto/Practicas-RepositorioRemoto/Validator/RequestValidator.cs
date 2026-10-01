using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Validator.Common;
using Serilog;

namespace Practicas_RepositorioRemoto.Validator;

// ─── REQUEST VALIDATOR ───

/// <summary>
/// Validador de la peticion de creacion de usuario.
/// </summary>
public class RequestValidator : IValidador<CreateUserRequest>
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(RequestValidator));

    /// <summary>
    /// Valida la peticion aplicando todas las reglas de dominio.
    /// </summary>
    /// <param name="entidad">Peticion a validar</param>
    /// <returns>Result con la peticion validada o error si no es valida</returns>
    public Result<CreateUserRequest, DomainError> Validar(CreateUserRequest entidad)
    {
        var emptyCheckResult = entidad.CheckEmptyOrWhiteSpace();
        if (emptyCheckResult.IsFailure)
        {
            _logger.Warning("Error de validación: la petición de creación de usuario tiene campos vacíos.");
            return Result.Failure<CreateUserRequest, DomainError>(emptyCheckResult.Error);
        }

        var regexCheckResult = entidad.CheckRegex();
        if (regexCheckResult.IsFailure)
        {
            _logger.Warning("Error de validación: la petición de creación de usuario tiene formatos inválidos.");
            return Result.Failure<CreateUserRequest, DomainError>(regexCheckResult.Error);
        }

        return Result.Success<CreateUserRequest, DomainError>(entidad);
    }
}
