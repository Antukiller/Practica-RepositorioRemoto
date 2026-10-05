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
    private readonly AppDbContextPostgre _contextPostgre = contextPostgre;
    
    public async Task<IEnumerable<User>> GetAllAsync() {
        var models = await _contextPostgre.Users
            .OrderBy(e => e.Id)
            .ToListAsync();
        return models;
    }

    public async Task<Result<User, DomainError>> GetByIdAsync(int id) {
        var model = await _contextPostgre.Users.FindAsync(id);
        if (model is null) {
            _logger.Debug($"Error al intentar encontrar la entidad con el id: {id}.");
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User not found", id));
        }
        _logger.Debug($"Se ha encontrado con exito la entidad con el id: {id}.");
        return Result.Success<User, DomainError>(model);
    }

    public async Task<Result<User, DomainError>> CreateAsync(User entity) {
        try {
            _contextPostgre.Users.Add(entity);
            await _contextPostgre.SaveChangesAsync();
            
            _logger.Debug($"Se ha registrado correctamente la nueva entidad.");
            return Result.Success<User, DomainError>(entity);
        } catch ( Exception e ) {
            _logger.Debug($"Error al intentar registrar la entidad.");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }

    public async Task<Result<User, DomainError>> UpdateAsync(int id, User entity) {
        var user = await _contextPostgre.Users.FindAsync(id);
        if (user is null) {
            _logger.Debug($"Error al intentar encontrar la entidad con el id: {id}.");
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User not found", id));
        }
        if(user.IsDeleted) {
            _logger.Debug("Error la entidad ya esta borrada.");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError("La entidad ya esta borrada."));
        }

        try {
            var updatedUser = user with {
                Id = id,  
                Name = entity.Name,
                UserName = entity.UserName,
                Email = entity.Email,
                Address = entity.Address,
                Phone = entity.Phone,
                Website = entity.Website,
                Company = entity.Company,
                CreateAt = entity.CreateAt,
                UpdateAt = entity.UpdateAt,
                DeleteAt = entity.DeleteAt,
                IsDeleted = entity.IsDeleted
            };
            _contextPostgre.Entry(user).CurrentValues.SetValues(updatedUser);
            await _contextPostgre.SaveChangesAsync();
            
            _logger.Debug($"Se ha actualizado con exito la entidad con el id: {id}.");
            return Result.Success<User, DomainError>(updatedUser);
        } catch (Exception e) {
            _logger.Debug($"Error al intentar actualizar la entidad.");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }

    /// <summary>
    /// Elimina un usuario de forma lógica, marcando <see cref="User.IsDeleted"/> a true
    /// y registrando <see cref="User.DeleteAt"/>. La fila permanece en la tabla.
    /// </summary>
    /// <param name="id">Identificador del usuario a eliminar</param>
    /// <returns>El usuario marcado como eliminado, o un error si no existe</returns>
    public async Task<Result<User, DomainError>> DeleteAsync(int id) {
        var user = await _contextPostgre.Users.FindAsync(id);
        if (user is null) {
            _logger.Debug($"Error al intentar encontrar la entidad con el id: {id}.");
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User not found", id));
        }

        try {
            var deletedUser = user with {
                IsDeleted = true,
                DeleteAt = DateTime.UtcNow
            };
            _contextPostgre.Entry(user).CurrentValues.SetValues(deletedUser);
            await _contextPostgre.SaveChangesAsync();

            _logger.Debug($"Se ha eliminado con exito la entidad con el id: {id}.");
            return Result.Success<User, DomainError>(deletedUser);
        } catch (Exception e) {
            _logger.Debug($"Error al intentar eliminar la entidad.");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }
}