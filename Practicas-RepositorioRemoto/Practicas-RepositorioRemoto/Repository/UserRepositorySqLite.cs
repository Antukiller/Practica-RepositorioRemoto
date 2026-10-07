using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Practicas_RepositorioRemoto.Entity;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Serilog;
using Serilog.Core;

namespace Practicas_RepositorioRemoto.Repository;

/// <summary>
/// 
/// </summary>
public class UserRepositorySqLite(AppDbContext contextSqlite) : IUserRepository {

    private readonly ILogger _log = Log.ForContext<UserRepositorySqLite>();

    public async Task<IEnumerable<User>> GetAllAsync() {
        _log.Information("Getting all the users");
        return await contextSqlite.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<Result<User, DomainError>> GetByIdAsync(int id) {
        _log.Information($"Getting the user by id: {id}");
        var user = await contextSqlite.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        return user is not null
            ? Result.Success<User, DomainError>(user)
            : Result.Failure<User, DomainError>(new DomainError.NotFound("User not found", id));
    }
    

    public async Task<Result<User, DomainError>> CreateAsync(User value) {
        try {
            _log.Information("Creating a new user");
            contextSqlite.Users.Add(value);
            await contextSqlite.SaveChangesAsync();
            _log.Information("The new user has been created successfully");
            return Result.Success<User, DomainError>(value);
        } catch (Exception e) {
            _log.Error($"ERROR while creating the new user: {e.Message}");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }

    public async Task<Result<User, DomainError>> UpdateAsync(int id, User value) {
        _log.Information($"Updating the user by id: {id}");
        var user = await contextSqlite.Users.FindAsync(id);
        if (user == null)
            return Result.Failure<User, DomainError>(
                new DomainError.NotFound("User not found", id));
        if(user.IsDeleted) {
            _log.Debug("Error la entidad ya esta borrada.");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError("La entidad ya esta borrada."));
        }
        try {
            var updatedUser = user with {
                Name = value.Name,
                UserName = value.UserName,
                Email = value.Email,
                Address = value.Address,
                Phone = value.Phone,
                Website = value.Website,
                Company = value.Company,
                UpdateAt = DateTime.UtcNow,
                IsDeleted = value.IsDeleted
            };
            contextSqlite.Entry(user).CurrentValues.SetValues(updatedUser);
            await contextSqlite.SaveChangesAsync();
            _log.Information("The user has been updated successfully");
            return Result.Success<User, DomainError>(updatedUser);
        } catch (Exception e) {
            _log.Error($"ERROR while updating the user: {e.Message}");
            return Result.Failure<User, DomainError>(
                new DomainError.DatabaseError(e.Message));
        }
    }

    public async Task<Result<User, DomainError>> DeleteAsync(int id) {
        _log.Information($"Deleting the user with id: {id}");
        var user = await contextSqlite.Users.FindAsync(id);
        if (user == null || user.IsDeleted) 
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User not found", id));
        try {
            var delUser = user with {
                IsDeleted = true,
                DeleteAt = DateTime.UtcNow
            };
            contextSqlite.Entry(user).CurrentValues.SetValues(delUser);
            await contextSqlite.SaveChangesAsync();
            _log.Information("The user has been deleted successfully");
            return Result.Success<User, DomainError>(delUser);
        } catch (Exception e) {
            _log.Error($"ERROR while deleting the user: {e.Message}");     
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }

    /// <summary>
    /// Elimina físicamente todos los usuarios de la tabla.
    /// </summary>
    /// <remarks>
    /// El borrado lógico de <see cref="DeleteAsync(int)"/> no sirve para la sincronización:
    /// dejaría registros con IsDeleted = true cuya clave primaria impediría reinsertarlos.
    /// ExecuteDeleteAsync borra las filas sin pasar por el change tracker.
    /// </remarks>
    /// <returns>El número de registros eliminados.</returns>
    public async Task<Result<int, DomainError>> DeleteAllAsync() {
        _log.Information("Deleting all the users");
        try {
            var eliminados = await contextSqlite.Users.ExecuteDeleteAsync();
            _log.Information($"All users have been deleted successfully. Rows removed: {eliminados}");
            return Result.Success<int, DomainError>(eliminados);
        } catch (Exception e) {
            _log.Error($"ERROR while deleting all users: {e.Message}");
            return Result.Failure<int, DomainError>(new DomainError.DatabaseError(e.Message));
        }
    }
}