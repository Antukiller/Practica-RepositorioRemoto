using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Errors;
using Serilog;

namespace Practicas_RepositorioRemoto.Validator;

// ─── ADDRESS VALIDATOR ───

/// <summary>
/// Validador de las propiedades de una direccion.
/// </summary>
public static class AddressValidator
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(AddressValidator));

    private static readonly Regex _regexStreet = new(@"^[\p{L}\p{N}\s.,#'\-]{2,100}$");
    private static readonly Regex _regexSuite = new(@"^[\p{L}\p{N}\s.,#'\-]{1,50}$");
    private static readonly Regex _regexCity = new(@"^[\p{L}\s.'\-]{2,100}$");
    private static readonly Regex _regexZipCode = new(@"^[0-9]{5}$");

    /// <summary>
    /// Comprueba que los campos obligatorios de la direccion no esten vacios.
    /// </summary>
    public static Result<bool, DomainError> CheckEmptyOrWhiteSpace(this AddressDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Street))
        {
            _logger.Warning("Error de validación: El campo Street de la dirección está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.Street", "La calle no puede estar vacía."));
        }

        if (string.IsNullOrWhiteSpace(item.City))
        {
            _logger.Warning("Error de validación: El campo City de la dirección está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.City", "La ciudad no puede estar vacía."));
        }

        if (string.IsNullOrWhiteSpace(item.ZipCode))
        {
            _logger.Warning("Error de validación: El campo ZipCode de la dirección está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.ZipCode", "El código postal no puede estar vacío."));
        }

        if (item.Geo is null)
        {
            _logger.Warning("Error de validación: El objeto Geo de la dirección es nulo.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.Geo", "La geolocalización es obligatoria."));
        }

        return item.Geo.CheckEmptyOrWhiteSpace();
    }

    /// <summary>
    /// Comprueba el formato de los campos de la direccion y de su geolocalizacion.
    /// </summary>
    public static Result<bool, DomainError> CheckRegex(this AddressDto item)
    {
        if (!_regexStreet.IsMatch(item.Street))
        {
            _logger.Warning("Error de validación: Formato del campo Street de la dirección no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.Street", "El formato de la calle no es válido."));
        }

        // Suite es opcional: si viene informado se valida el formato.
        if (!string.IsNullOrWhiteSpace(item.Suite) && !_regexSuite.IsMatch(item.Suite))
        {
            _logger.Warning("Error de validación: Formato del campo Suite de la dirección no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.Suite", "El formato del número o planta de la dirección no es válido."));
        }

        if (!_regexCity.IsMatch(item.City))
        {
            _logger.Warning("Error de validación: Formato del campo City de la dirección no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.City", "El formato de la ciudad no es válido."));
        }

        if (!_regexZipCode.IsMatch(item.ZipCode))
        {
            _logger.Warning("Error de validación: Formato del campo ZipCode de la dirección no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Address.ZipCode", "El código postal debe tener 5 dígitos."));
        }

        return item.Geo.CheckRegex();
    }
}
