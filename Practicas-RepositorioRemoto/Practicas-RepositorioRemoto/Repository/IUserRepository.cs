using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Repository.Common;

namespace Practicas_RepositorioRemoto.Repository;

public interface IUserRepository : ICrudRepository<int, User> {

    /// <summary>
    /// Elimina físicamente todos los registros de la tabla de usuarios.
    /// </summary>
    /// <remarks>
    /// A diferencia de <see cref="ICrudRepository{TKey,TValue}.DeleteAsync"/>, que realiza un
    /// borrado lógico registro a registro, este método vacía la tabla por completo. Es lo que
    /// necesita la sincronización periódica antes de reinsertar los datos de la API.
    /// </remarks>
    /// <returns>
    /// El número de registros eliminados, o un <see cref="DomainError.DatabaseError"/> si falla.
    /// </returns>
    Task<Result<int, DomainError>> DeleteAllAsync();
}