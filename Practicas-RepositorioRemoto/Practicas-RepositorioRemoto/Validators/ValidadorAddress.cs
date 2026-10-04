using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Validators;

// ─── VALIDATION EXTENSIONS ───
/// <summary>
///     Métodos de extensión para validar las propiedades de una dirección.
/// </summary>
public static class ValidadorAddressExtensions {
    /// <summary>Valida que la calle sea obligatoria (no nula ni en blanco).</summary>
    public static bool IsValidStreet(this string calle) {
        return calle.IsNotBlank();
    }

    /// <summary>Valida que el bloque/piso sea obligatorio (no nulo ni en blanco).</summary>
    public static bool IsValidSuite(this string suite) {
        return suite.IsNotBlank();
    }

    /// <summary>Valida que la ciudad sea obligatoria (no nula ni en blanco).</summary>
    public static bool IsValidCity(this string ciudad) {
        return ciudad.IsNotBlank();
    }

    /// <summary>Valida que la geolocalización exista y tenga coordenadas con formato numérico válido.</summary>
    public static bool IsValidGeo(this Geo geo) {
        if (geo is null) return false;

        return geo.Lat.IsValidCoordinate() && geo.Lng.IsValidCoordinate();
    }
}

// ─── ADDRESS VALIDATOR ───

/// <summary>
///     Validador de direcciones que implementa las reglas de dominio.
/// </summary>
public class ValidadorAddress : IValidador<Address> {
    /// <summary>Valida una dirección aplicando todas las reglas de dominio.</summary>
    public Result<Address, DomainError> Validar(Address direccion) {
        var errores = new List<string>();

        if (direccion is null)
            return Result.Failure<Address, DomainError>(
                new DomainError.ValidationError(nameof(Address), "La dirección es obligatoria."));

        if (!direccion.Street.IsValidStreet())
            errores.Add("La calle es obligatoria y no puede estar en blanco.");

        if (!direccion.Suite.IsValidSuite())
            errores.Add("El bloque/piso es obligatorio y no puede estar en blanco.");

        if (!direccion.City.IsValidCity())
            errores.Add("La ciudad es obligatoria y no puede estar en blanco.");

        if (!direccion.ZipCode.IsValidZipCode())
            errores.Add("El código postal no es válido (5 dígitos, con extensión opcional de 4).");

        if (!direccion.Geo.IsValidGeo())
            errores.Add("La geolocalización es obligatoria y sus coordenadas deben tener formato numérico válido.");

        if (errores.Any())
            return Result.Failure<Address, DomainError>(
                new DomainError.ValidationError(nameof(Address), string.Join(" ", errores)));

        return Result.Success<Address, DomainError>(direccion);
    }
}