using System.Globalization;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Errors;
using Serilog;

namespace Practicas_RepositorioRemoto.Validator;

// ─── GEO VALIDATOR ───

/// <summary>
/// Validador de la geolocalizacion contenida en una direccion.
/// </summary>
public static class GeoValidator
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(GeoValidator));

    private static readonly Regex _regexLat = new(@"^-?\d{1,2}([.,]\d+)?$");
    private static readonly Regex _regexLng = new(@"^-?\d{1,3}([.,]\d+)?$");

    /// <summary>
    /// Comprueba que la latitud y la longitud no esten vacias.
    /// </summary>
    public static Result<bool, DomainError> CheckEmptyOrWhiteSpace(this GeoDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Lat))
        {
            _logger.Warning("Error de validación: El campo Lat de la geolocalización está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Geo.Lat", "La latitud no puede estar vacía."));
        }

        if (string.IsNullOrWhiteSpace(item.Lng))
        {
            _logger.Warning("Error de validación: El campo Lng de la geolocalización está vacío.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Geo.Lng", "La longitud no puede estar vacía."));
        }

        return Result.Success<bool, DomainError>(true);
    }

    /// <summary>
    /// Comprueba que la latitud y la longitud sean numéricas y estén dentro de rango.
    /// </summary>
    public static Result<bool, DomainError> CheckRegex(this GeoDto item)
    {
        if (!_regexLat.IsMatch(item.Lat))
        {
            _logger.Warning("Error de validación: Formato del campo Lat no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Geo.Lat", "El formato de la latitud no es válido."));
        }

        if (!_regexLng.IsMatch(item.Lng))
        {
            _logger.Warning("Error de validación: Formato del campo Lng no válido.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Geo.Lng", "El formato de la longitud no es válido."));
        }

        if (!decimal.TryParse(item.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || lat is < -90m or > 90m)
        {
            _logger.Warning("Error de validación: El campo Lat está fuera de rango.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Geo.Lat", "La latitud debe estar entre -90 y 90."));
        }

        if (!decimal.TryParse(item.Lng, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)
            || lng is < -180m or > 180m)
        {
            _logger.Warning("Error de validación: El campo Lng está fuera de rango.");
            return Result.Failure<bool, DomainError>(
                new DomainError.ValidationError("Geo.Lng", "La longitud debe estar entre -180 y 180."));
        }

        return Result.Success<bool, DomainError>(true);
    }
}
