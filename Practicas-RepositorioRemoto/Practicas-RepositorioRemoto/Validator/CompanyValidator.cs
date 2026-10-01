using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Errors;
using Serilog;

namespace Practicas_RepositorioRemoto.Validator;

// ─── COMPANY VALIDATOR ───

/// <summary>
/// Validador de las propiedades de una empresa.
/// </summary>
public static class CompanyValidator
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(CompanyValidator));

    private static readonly Regex _regexName = new(@"^[\p{L}\p{N}\s.,#'\-]{2,100}$");
    private static readonly Regex _regexCatchPhrase = new(@"^[\p{L}\p{N}\s.,#'\-]{3,150}$");
    private static readonly Regex _regexBs = new(@"^[\p{L}\p{N}\s.,#'\-]{3,150}$");

    /// <summary>
    /// Comprueba que los campos de la empresa no esten vacios.
    /// </summary>
    public static Result<bool, DomainError> CheckEmptyOrWhiteSpace(this CompanyDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _logger.Warning("Error de validación: El campo Name de la empresa está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Company.Name", "El nombre de la empresa no puede estar vacío."));
        }

        if (string.IsNullOrWhiteSpace(item.CatchPhrase))
        {
            _logger.Warning("Error de validación: El campo CatchPhrase de la empresa está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Company.CatchPhrase", "El eslogan de la empresa no puede estar vacío."));
        }

        if (string.IsNullOrWhiteSpace(item.Bs))
        {
            _logger.Warning("Error de validación: El campo Bs de la empresa está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Company.Bs", "La actividad de la empresa no puede estar vacía."));
        }

        return Result.Success<bool, DomainError>(true);
    }

    /// <summary>
    /// Comprueba el formato de los campos de la empresa.
    /// </summary>
    public static Result<bool, DomainError> CheckRegex(this CompanyDto item)
    {
        if (!_regexName.IsMatch(item.Name))
        {
            _logger.Warning("Error de validación: Formato del campo Name de la empresa no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Company.Name", "El formato del nombre de la empresa no es válido."));
        }

        if (!_regexCatchPhrase.IsMatch(item.CatchPhrase))
        {
            _logger.Warning("Error de validación: Formato del campo CatchPhrase no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Company.CatchPhrase", "El formato del eslogan no es válido."));
        }

        if (!_regexBs.IsMatch(item.Bs))
        {
            _logger.Warning("Error de validación: Formato del campo Bs no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Company.Bs", "El formato de la actividad no es válido."));
        }

        return Result.Success<bool, DomainError>(true);
    }
}
