using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Errors;

namespace Practicas_RepositorioRemoto.Repository.Common;

/// <summary>
/// Interfaz generica para las operaciones CRUD
/// </summary>
public interface ICrudRepository<Tkey, Tvalue> where Tvalue : class {
    /// <summary>
    /// Devuelve todas las entidades
    /// </summary>
    Task<IEnumerable<Tvalue>> GetAllAsync();
    /// <summary>
    /// Busca una entidad por su id
    /// </summary>
    Task<Result<Tvalue, DomainError>> GetByIdAsync(Tkey id);
    /// <summary>
    /// Añade un nuevo registro
    /// </summary>
    Task<Result<Tvalue, DomainError>> CreateAsync(Tvalue value);
    /// <summary>
    /// Actualiza un registro
    /// </summary>
    Task<Result<Tvalue, DomainError>> UpdateAsync(Tkey id, Tvalue value);
    /// <summary>
    /// Elimina un registro
    /// </summary>
    Task<Result<Tvalue, DomainError>> DeleteAsync(Tkey id);
}