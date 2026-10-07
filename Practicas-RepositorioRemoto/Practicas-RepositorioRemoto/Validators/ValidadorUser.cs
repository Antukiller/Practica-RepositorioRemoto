using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Interfaces;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Validators;

// ─── USER VALIDATOR ───

/// <summary>
///     Validador de usuarios que implementa las reglas de dominio.
/// </summary>
/// <remarks>
///     Delega en <see cref="IValidador{T}" /> la validación de los objetos anidados
///     (<see cref="Address" /> y <see cref="Company" />) e incorpora sus errores en
///     la lista acumulada, de forma que un fallo en un anidado nunca se pierde.
/// </remarks>
/// <param name="validadorAddress">Validador de la dirección del usuario</param>
/// <param name="validadorCompany">Validador de la compañía del usuario</param>
public class ValidadorUser(
    IValidador<Address> validadorAddress,
    IValidador<Company> validadorCompany) : IValidador<User>, ITransientService {
    /// <summary>Valida un usuario aplicando todas las reglas de dominio.</summary>
    /// <param name="user">Usuario a validar</param>
    /// <returns>
    ///     <see cref="Result.Success{T, E}" /> si cumple todas las reglas;
    ///     <see cref="Result.Failure{T, E}" /> con un <see cref="Validation" />
    ///     que acumula todos los errores detectados.
    /// </returns>
    public Result<User, DomainError> Validar(User user) {
        if (user is null)
            return Result.Failure<User, DomainError>(
                new Validation(["El usuario es obligatorio."]));

        var errores = new List<string>();

        if (user.Id < 0)
            errores.Add("El identificador del usuario no puede ser negativo.");

        if (!user.Name.IsNotBlank())
            errores.Add("El nombre es obligatorio y no puede estar en blanco.");

        if (!user.UserName.IsNotBlank())
            errores.Add("El alias de usuario es obligatorio.");

        if (!user.UserName.IsValidUserNameFormat())
            errores.Add("El alias de usuario solo admite caracteres alfanuméricos, '_' o '.'.");

        if (!user.Email.IsValidEmail())
            errores.Add("El email es obligatorio y debe tener un formato válido.");

        if (!user.Phone.IsValidSpanishPhone())
            errores.Add("El teléfono es obligatorio y debe tener formato español.");

        if (!user.Website.IsNotBlank())
            errores.Add("El sitio web es obligatorio.");

        if (!user.Website.IsValidWebsite())
            errores.Add("El sitio web no tiene un formato de URL válido.");

        if (user.CreateAt > DateTime.Now)
            errores.Add("La fecha de creación no puede ser futura.");

        if (user.UpdateAt < user.CreateAt)
            errores.Add("La fecha de actualización no puede ser anterior a la de creación.");

        if (!user.IsDeleted && user.DeleteAt != default)
            errores.Add("Un usuario no eliminado no debe tener fecha de eliminación.");

        // Los validadores anidados inyectan sus errores en la misma lista acumulada.
        var resultAddress = validadorAddress.Validar(user.Address);
        if (resultAddress.IsFailure)
            AñadirErrores(errores, resultAddress.Error);

        var resultCompany = validadorCompany.Validar(user.Company);
        if (resultCompany.IsFailure)
            AñadirErrores(errores, resultCompany.Error);

        return errores.Any()
            ? Result.Failure<User, DomainError>(new Validation(errores))
            : Result.Success<User, DomainError>(user);
    }

    /// <summary>
    ///     Traslada a la lista acumulada los errores de una validación anidada.
    /// </summary>
    /// <remarks>
    ///     Se cubren todos los subtipos de <see cref="DomainError" /> y no solo uno: si el
    ///     validador anidado devolviera un tipo distinto, el error se descartaría en
    ///     silencio y el usuario se daría por válido pese a tener la dirección o la
    ///     compañía corrupta.
    /// </remarks>
    /// <param name="destino">Lista donde se acumulan los mensajes de error</param>
    /// <param name="error">Error devuelto por el validador anidado</param>
    private static void AñadirErrores(List<string> destino, DomainError error) {
        switch (error) {
            case Validation validacion:
                destino.AddRange(validacion.Errors);
                break;
            case DomainError.ValidationError validacionCampo:
                destino.Add(validacionCampo.errorMessage);
                break;
            default:
                destino.Add("Se ha producido un error de validación inesperado.");
                break;
        }
    }
}