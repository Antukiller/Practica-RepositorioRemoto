using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Validators;

// ─── USER VALIDATOR ───

public class ValidadorUser(
    IValidador<Address> validadorAddress,
    IValidador<Company> validadorCompany) : IValidador<User> {
    public Result<User, DomainError> Validar(User user) {
        var errores = new List<string>();

        if (user is null)
            return Result.Failure<User, DomainError>(
                new DomainError.ValidationError(nameof(User), "El usuario es obligatorio."));

        if (!user.Name.IsValidUserNameFormat())
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

        // Validaciones anidadas extrayendo el error exacto si falla
        var resultAddress = validadorAddress.Validar(user.Address);
        if (resultAddress.IsFailure)
            if (resultAddress.Error is DomainError.ValidationError validationErr)
                errores.Add(validationErr.errorMessage);

        var resultCompany = validadorCompany.Validar(user.Company);
        if (resultCompany.IsFailure)
            if (resultCompany.Error is DomainError.ValidationError validationErr)
                errores.Add(validationErr.errorMessage);

        if (errores.Any())
            return Result.Failure<User, DomainError>(
                new DomainError.ValidationError(nameof(User), string.Join(" ", errores)));

        return Result.Success<User, DomainError>(user);
    }
}