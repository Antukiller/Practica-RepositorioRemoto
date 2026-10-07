using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Models;
using Refit;

namespace Practicas_RepositorioRemoto.Api;

/// <summary>
/// Interfaz que define los endpoints de JSONPlaceholder.
/// Refit genera la implementación en tiempo de compilación
/// a partir de los atributos [Get], [Post], [Put], [Delete].
/// </summary>
[Headers("Content-Type: application/json")]
public interface IJsonPlaceHolder {
    
    /// <summary>
    /// GET /users - Obtiene todos los usuarios.
    /// </summary>
    [Get("/users")]
    Task<List<User>> GetUsersAsync();
    
    /// <summary>
    /// GET /users/{id} - Obtiene un usuario por su ID.
    /// </summary>    
    [Get("/users/{id}")]
    Task<User?> GetUsersByIdAsync(int id);
    
    /// <summary>
    /// POST /users - Crea un nuevo usuario.
    /// El servidor asigna el Id automáticamente.
    /// </summary>
    [Post("/users")]
    Task<User> CreateUserAsync([Body] CreateUserRequest request);
    
    /// <summary>
    /// PUT /users/{id} - Actualiza un usuario.
    /// </summary>
    [Put("/users/{id}")]
    Task<User> UpdateUserAsync(int id, [Body] UpdateUserRequest request);
    
    /// <summary>
    /// Delete /user/{id} Elimina usuario
    /// </summary>
    [Delete("/users/{id}")]
    Task DeleteUserAsync(int id);
    
}