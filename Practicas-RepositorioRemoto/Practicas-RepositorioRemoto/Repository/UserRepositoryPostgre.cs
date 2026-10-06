using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Practicas_RepositorioRemoto.Entity;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Interfaces;
using Practicas_RepositorioRemoto.Models;
using Serilog;

namespace Practicas_RepositorioRemoto.Repository;

public class UserRepositoryPostgre(AppDbContextPostgre contextPostgre) : IUserRepository, IScopedService {
    private readonly ILogger _logger = Log.ForContext<UserRepositoryPostgre>();

    /// <summary>
    /// Obtiene todos los usuarios activos (no eliminados).
    /// </summary>
    public async Task<IEnumerable<User>> GetAllAsync() {
        return await contextPostgre.Users
            .Where(e => !e.IsDeleted) // Filtramos los borrados lógicamente
            .OrderBy(e => e.Id)
            .AsNoTracking() // AsNoTracking para optimizar lecturas
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un usuario activo por su ID.
    /// </summary>
    public async Task<Result<User, DomainError>> GetByIdAsync(int id) {
        var model = await contextPostgre.Users.FindAsync(id);

        if (model is null || model.IsDeleted) {
            _logger.Debug("Error al intentar encontrar la entidad activa con el id: {Id}.", id);
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User not found", id));
        }

        _logger.Debug("Se ha encontrado con éxito la entidad con el id: {Id}.", id);
        return Result.Success<User, DomainError>(model);
    }

    /// <summary>
    /// Registra un nuevo usuario asignándole la fecha de creación en el servidor.
    /// </summary>
    public async Task<Result<User, DomainError>> CreateAsync(User entity) {
        try {
            var newUser = entity with {
                CreateAt = DateTime.UtcNow,
                IsDeleted = false
            };

            contextPostgre.Users.Add(newUser);
            await contextPostgre.SaveChangesAsync();

            _logger.Debug("Se ha registrado correctamente la nueva entidad con ID: {Id}.", newUser.Id);
            return Result.Success<User, DomainError>(newUser);
        }
        catch (Exception e) {
            _logger.Error(e, "Error al intentar registrar la entidad.");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }

    /// <summary>
    /// Actualiza un usuario existente refrescando su fecha de modificación.
    /// </summary>
    public async Task<Result<User, DomainError>> UpdateAsync(int id, User entity) {
        var user = await contextPostgre.Users.FindAsync(id);

        if (user is null || user.IsDeleted) {
            _logger.Debug("Error al intentar encontrar la entidad activa con el id: {Id}.", id);
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User not found or deleted", id));
        }

        try {
            // 1. Recreamos el record inmutable respetando el ID y la fecha de creación original
            var updatedUser = user with {
                Name = entity.Name,
                UserName = entity.UserName,
                Email = entity.Email,
                Address = entity.Address,
                Phone = entity.Phone,
                Website = entity.Website,
                Company = entity.Company,
                UpdateAt = DateTime.UtcNow
            };

            // 2. Desenganchamos la entidad antigua y marcamos la nueva como modificada
            contextPostgre.Entry(user).State = EntityState.Detached;
            contextPostgre.Users.Update(updatedUser);

            await contextPostgre.SaveChangesAsync();

            _logger.Debug("Se ha actualizado con éxito la entidad con el id: {Id}.", id);
            return Result.Success<User, DomainError>(updatedUser);
        }
        catch (Exception e) {
            _logger.Error(e, "Error al intentar actualizar la entidad con id: {Id}.", id);
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }

    /// <summary>
    /// Realiza un borrado lógico (Soft Delete) marcando la entidad como eliminada.
    /// </summary>
    public async Task<Result<User, DomainError>> DeleteAsync(int id) {
        var user = await contextPostgre.Users.FindAsync(id);

        if (user is null || user.IsDeleted) {
            _logger.Debug("Error al intentar eliminar: la entidad no existe o ya está borrada. ID: {Id}.", id);
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User not found or already deleted", id));
        }

        try {
            var deletedUser = user with {
                IsDeleted = true,
                DeleteAt = DateTime.UtcNow
            };

            contextPostgre.Entry(user).State = EntityState.Detached;
            contextPostgre.Users.Update(deletedUser);

            await contextPostgre.SaveChangesAsync();

            _logger.Debug("Se ha eliminado lógicamente con éxito la entidad con el id: {Id}.", id);
            return Result.Success<User, DomainError>(deletedUser); // Devuelve deletedUser
        }
        catch (Exception e) {
            _logger.Error(e, "Error al intentar eliminar la entidad con id: {Id}.", id);
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }

    /// <summary>
    /// Elimina físicamente todos los usuarios de la tabla.
    /// </summary>
    /// <remarks>
    /// El borrado lógico de <see cref="DeleteAsync"/> no sirve para la sincronización: dejaría
    /// registros con <c>IsDeleted = true</c> cuya clave primaria impediría reinsertarlos.
    /// Por eso se usa <c>ExecuteDeleteAsync</c>, que borra las filas sin pasar por el change tracker.
    /// </remarks>
    /// <returns>El número de registros eliminados.</returns>
    public async Task<Result<int, DomainError>> DeleteAllAsync() {
        try {
            var eliminados = await contextPostgre.Users.ExecuteDeleteAsync();

            _logger.Debug("Tabla de usuarios vaciada. Registros eliminados: {Total}", eliminados);
            return Result.Success<int, DomainError>(eliminados);
        }
        catch (Exception e) {
            _logger.Error(e, "Error al eliminar todos los usuarios.");
            return Result.Failure<int, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }
}
