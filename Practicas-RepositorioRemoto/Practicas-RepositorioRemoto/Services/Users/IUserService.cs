using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Services;

public interface IUserService {
    /// <summary>
    ///     Obtiene los usuarios de la base de datos local.
    ///     Si está vacía, los obtiene de la API y los almacena.
    /// </summary>
    /// <returns>Usuarios encontrados o un error.</returns>
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>
    ///     Busca un usuario en caché, base de datos y API, en ese orden.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Usuario encontrado o un error.</returns>
    Task<Result<User, DomainError>> GetByIdAsync(int id);

    /// <summary>
    ///     Válida y crea un usuario en la API REST y en la base de datos local.
    ///     Emite una notificación cuando se completa la operación.
    /// </summary>
    /// <param name="request">Datos del nuevo usuario.</param>
    /// <returns>Usuario creado o un error.</returns>
    Task<Result<User, DomainError>> CreateAsync(CreateUserRequest request);

    /// <summary>
    ///     Válida y actualiza un usuario en la API REST y en la base de datos local.
    ///     Emite una notificación cuando se completa la operación.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="request">Nuevos datos del usuario.</param>
    /// <returns>Usuario actualizado o un error.</returns>
    Task<Result<User, DomainError>> UpdateAsync(int id, UpdateUserRequest request);

    /// <summary>
    ///     Elimina un usuario de la API, de la base de datos local y de la caché.
    ///     Emite una notificación cuando se completa la operación.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Usuario eliminado o un error.</returns>
    Task<Result<User, DomainError>> DeleteAsync(int id);
}