using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Errors;
using Serilog;

namespace Practicas_RepositorioRemoto.Validator;

// ─── CREATE USER REQUEST VALIDATOR ───

/// <summary>
/// Validador de las propiedades de la peticion de creacion de usuario.
/// </summary>
public static class CreateUserRequestValidator
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(CreateUserRequestValidator));

    private static readonly Regex _regexName = new(@"^[\p{L}\s.\-]{2,50}$");
    private static readonly Regex _regexUserName = new(@"^[A-Za-z0-9._\-]{3,30}$");
    private static readonly Regex _regexEmail = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    private static readonly Regex _regexPhone = new(@"^(\+[1-9]\d{0,2}[\s\.-]?)?(\(?\d+\)?[\s\.-]?){2,8}(\s?(x|ext|extension)\s?\d{1,8})?$");
    private static readonly Regex _regexWebsite = new(@"^(https?:\/\/)?(www\.)?[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}(\/.*)?$");

    /// <summary>
    /// Comprueba que los campos obligatorios de la peticion y sus objetos anidados no esten vacios.
    /// </summary>
    public static Result<bool, DomainError> CheckEmptyOrWhiteSpace(this CreateUserRequest item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _logger.Warning("Error de validación: El campo Name está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Name", "El nombre no puede estar vacío."));
        }

        if (string.IsNullOrWhiteSpace(item.UserName))
        {
            _logger.Warning("Error de validación: El campo UserName está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("UserName", "El nombre de usuario no puede estar vacío."));
        }

        if (string.IsNullOrWhiteSpace(item.Email))
        {
            _logger.Warning("Error de validación: El campo Email está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Email", "El correo electrónico no puede estar vacío."));
        }

        if (string.IsNullOrWhiteSpace(item.Phone))
        {
            _logger.Warning("Error de validación: El campo Phone está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Phone", "El teléfono no puede estar vacío."));
        }

        if (string.IsNullOrWhiteSpace(item.Website))
        {
            _logger.Warning("Error de validación: El campo Website está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Website", "El sitio web no puede estar vacío."));
        }

        if (item.Address is null)
        {
            _logger.Warning("Error de validación: El objeto Address es nulo.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address", "La dirección es obligatoria."));
        }

        var addressResult = item.Address.CheckEmptyOrWhiteSpace();
        if (addressResult.IsFailure)
            return addressResult;

        if (item.Company is null)
        {
            _logger.Warning("Error de validación: El objeto Company es nulo.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Company", "La información de la empresa es obligatoria."));
        }

        return item.Company.CheckEmptyOrWhiteSpace();
    }

    /// <summary>
    /// Comprueba el formato de los campos de la peticion y de sus objetos anidados.
    /// </summary>
    public static Result<bool, DomainError> CheckRegex(this CreateUserRequest item)
    {
        if (!_regexName.IsMatch(item.Name))
        {
            _logger.Warning("Error de validación: Formato de Name no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Name", "El formato del nombre no es válido."));
        }

        if (!_regexUserName.IsMatch(item.UserName))
        {
            _logger.Warning("Error de validación: Formato de UserName no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("UserName", "El formato del nombre de usuario no es válido."));
        }

        if (!_regexEmail.IsMatch(item.Email))
        {
            _logger.Warning("Error de validación: Formato de Email no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Email", "El formato del correo electrónico no es válido."));
        }

        if (!_regexPhone.IsMatch(item.Phone))
        {
            _logger.Warning("Error de validación: Formato de Phone no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Phone", "El formato del teléfono no es válido."));
        }

        if (!_regexWebsite.IsMatch(item.Website))
        {
            _logger.Warning("Error de validación: Formato de Website no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Website", "El formato del sitio web no es válido."));
        }

        var addressResult = item.Address.CheckRegex();
        if (addressResult.IsFailure)
            return addressResult;

        return item.Company.CheckRegex();
    }
}
