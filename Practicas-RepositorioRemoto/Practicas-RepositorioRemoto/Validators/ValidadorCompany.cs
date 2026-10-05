using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Validators;

// ─── VALIDATION EXTENSIONS ───
/// <summary>
///     Métodos de extensión para validar las propiedades de una compañía.
/// </summary>
public static class ValidadorCompanyExtensions {
    /// <summary>Valida que el nombre de la compañía sea obligatorio (no nulo ni en blanco).</summary>
    public static bool IsValidCompanyName(this string nombre) {
        return nombre.IsNotBlank();
    }

    /// <summary>Valida que el eslogan sea obligatorio (no nulo ni en blanco).</summary>
    public static bool IsValidCatchPhrase(this string catchPhrase) {
        return catchPhrase.IsNotBlank();
    }

    /// <summary>Valida que el lema de negocio sea obligatorio (no nulo ni en blanco).</summary>
    public static bool IsValidBs(this string bs) {
        return bs.IsNotBlank();
    }
}

// ─── COMPANY VALIDATOR ───

/// <summary>
///     Validador de compañías que implementa las reglas de dominio.
/// </summary>
/// <remarks>
///     Acumula todos los errores de una compañía en un único
///     <see cref="Validation" />.
/// </remarks>
public class ValidadorCompany : IValidador<Company> {
    /// <summary>Valida una compañía aplicando todas las reglas de dominio.</summary>
    /// <param name="compania">Compañía a validar</param>
    /// <returns>
    ///     <see cref="Result.Success{T, E}" /> si cumple todas las reglas;
    ///     <see cref="Result.Failure{T, E}" /> con un <see cref="Validation" />
    ///     que acumula todos los errores detectados.
    /// </returns>
    public Result<Company, DomainError> Validar(Company compania) {
        if (compania is null)
            return Result.Failure<Company, DomainError>(
                new Validation(["La compañía es obligatoria."]));

        var errores = new List<string>();

        if (!compania.Name.IsValidCompanyName())
            errores.Add("El nombre de la compañía es obligatorio y no puede estar en blanco.");

        if (!compania.CatchPhrase.IsValidCatchPhrase())
            errores.Add("El eslogan es obligatorio y no puede estar en blanco.");

        if (!compania.Bs.IsValidBs())
            errores.Add("El lema de negocio es obligatorio y no puede estar en blanco.");

        return errores.Any()
            ? Result.Failure<Company, DomainError>(new Validation(errores))
            : Result.Success<Company, DomainError>(compania);
    }
}