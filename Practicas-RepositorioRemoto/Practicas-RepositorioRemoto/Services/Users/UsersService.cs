using System.Net;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Practicas_RepositorioRemoto.Api;
using Practicas_RepositorioRemoto.Cache.Common;
using Practicas_RepositorioRemoto.Config;
using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Interfaces;
using Practicas_RepositorioRemoto.Mapper;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Notifications;
using Practicas_RepositorioRemoto.Repository;
using Practicas_RepositorioRemoto.Validators;
using Refit;
using Serilog;

namespace Practicas_RepositorioRemoto.Services;

/// <summary>
///     Gestiona usuarios combinando los tres niveles de almacenamiento: caché,
///     base de datos local y API REST.
/// </summary>
/// <remarks>
///     <para>
///     Flujo de lectura: caché → BD local → API REST.
///     Flujo de escritura: API REST → BD local → notificación.
///     </para>
///     <para>
///     La escritura en caché se produce únicamente en la lectura (patrón cache-aside).
    ///     Una actualización no escribe en caché, pero sí la invalida, para que la siguiente
    ///     lectura repueble desde la base de datos.
    ///     </para>
/// </remarks>
/// <param name="validador">Validador de reglas de dominio del usuario.</param>
/// <param name="repository">Repositorio local de usuarios.</param>
/// <param name="cache">Caché de lectura.</param>
/// <param name="notificationService">Servicio reactivo de notificaciones.</param>
/// <param name="api">Cliente de la API remota.</param>
public class UsersService(
    IValidador<User> validador,
    IUserRepository repository,
    ICache cache,
    INotificationService notificationService,
    IJsonPlaceHolder api
) : IUserService, IScopedService {

    private readonly ILogger _logger = Log.ForContext<UsersService>();

    /// <inheritdoc cref="IUserService.GetAllAsync"/>
    public async Task<IEnumerable<User>> GetAllAsync() {
        var locales = await repository.GetAllAsync();
        if (locales.Any()) return locales;

        var remotos = await api.GetUsersAsync();

        foreach (var usuario in remotos) {
            var guardado = await repository.CreateAsync(DesdeApi(usuario));
            if (guardado.IsFailure) {
                _logger.Warning("No se pudo guardar el usuario {Id}: {Error}",
                    usuario.Id, guardado.Error);
            }
        }

        return remotos;
    }

    /// <inheritdoc cref="IUserService.ExportAsync"/>
    public Task<Result<string, DomainError>> ExportAsync() => ExportToJsonAsync();

    /// <inheritdoc cref="IUserService.GetByIdAsync"/>
    public async Task<Result<User, DomainError>> GetByIdAsync(int id) {
        var cacheado = await cache.GetAsync<User>(GetKeyUser(id));
        if (cacheado is not null) return Result.Success<User, DomainError>(cacheado);

        var local = await repository.GetByIdAsync(id);
        if (local.IsSuccess) {
            await cache.SetAsync(GetKeyUser(id), local.Value);
            return Result.Success<User, DomainError>(local.Value);
        }

        try {
            var remoto = await api.GetUsersByIdAsync(id);
            if (remoto is null) {
                return Result.Failure<User, DomainError>(new DomainError.NotFound("User", id));
            }

            var guardado = await repository.CreateAsync(DesdeApi(remoto));
            if (guardado.IsFailure) {
                return Result.Failure<User, DomainError>(guardado.Error);
            }

            await cache.SetAsync(GetKeyUser(id), guardado.Value);
            return Result.Success<User, DomainError>(guardado.Value);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User", id));
        }
        catch (ApiException ex) {
            _logger.Error(ex, "Error de API al obtener el usuario {Id}", id);
            return Result.Failure<User, DomainError>(
                new DomainError.ApiError((int)ex.StatusCode, ex.Message));
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error inesperado al obtener el usuario {Id}", id);
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(ex.Message));
        }
    }

    /// <inheritdoc cref="IUserService.CreateAsync"/>
    public async Task<Result<User, DomainError>> CreateAsync(CreateUserRequest request) {
        if (request is null)
            return Result.Failure<User, DomainError>(new DomainError.ValidationError(
                nameof(request), "La petición de creación no puede ser nula."));

        var usuario = request.ToModel();

        var validacion = validador.Validar(usuario);
        if (validacion.IsFailure) {
            return Result.Failure<User, DomainError>(validacion.Error);
        }

        try {
            var creado = await api.CreateUserAsync(request);

            var guardado = await repository.CreateAsync(usuario with { Id = creado.Id });
            if (guardado.IsFailure) {
                return Result.Failure<User, DomainError>(guardado.Error);
            }

            notificationService.NotificarCreado(guardado.Value.Id);
            return Result.Success<User, DomainError>(guardado.Value);
        }
        catch (ApiException ex) {
            _logger.Error(ex, "Error de API al crear el usuario");
            return Result.Failure<User, DomainError>(
                new DomainError.ApiError((int)ex.StatusCode, ex.Message));
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error inesperado al crear el usuario");
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(ex.Message));
        }
    }

    /// <inheritdoc cref="IUserService.UpdateAsync"/>
    public async Task<Result<User, DomainError>> UpdateAsync(int id, UpdateUserRequest request) {
        if (request is null)
            return Result.Failure<User, DomainError>(new DomainError.ValidationError(
                nameof(request), "La petición de actualización no puede ser nula."));

        if (id != request.Id) {
            return Result.Failure<User, DomainError>(new DomainError.ValidationError(
                "Id",
                $"El identificador de la ruta ({id}) no coincide con el del cuerpo ({request.Id})."));
        }

        var existe = await ComprobarExistenciaAsync(id);
        if (existe.IsFailure) return existe;

        var validacion = validador.Validar(request.ToModel());
        if (validacion.IsFailure) {
            return Result.Failure<User, DomainError>(validacion.Error);
        }

        try {
            await api.UpdateUserAsync(id, request);

            var guardado = await repository.UpdateAsync(id, request.ToModel());
            if (guardado.IsFailure) {
                return Result.Failure<User, DomainError>(guardado.Error);
            }

            await cache.RemoveAsync(GetKeyUser(id));
            notificationService.NotificarActualizado(id);
            return Result.Success<User, DomainError>(guardado.Value);
        }
        catch (ApiException ex) {
            _logger.Error(ex, "Error de API al actualizar el usuario {Id}", id);
            return Result.Failure<User, DomainError>(
                new DomainError.ApiError((int)ex.StatusCode, ex.Message));
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error inesperado al actualizar el usuario {Id}", id);
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(ex.Message));
        }
    }

    /// <inheritdoc cref="IUserService.DeleteAsync"/>
    public async Task<Result<User, DomainError>> DeleteAsync(int id) {
        var existe = await ComprobarExistenciaAsync(id);
        if (existe.IsFailure) return existe;

        try {
            await api.DeleteUserAsync(id);

            var borrado = await repository.DeleteAsync(id);
            if (borrado.IsFailure) {
                return Result.Failure<User, DomainError>(borrado.Error);
            }

            await cache.RemoveAsync(GetKeyUser(id));
            notificationService.NotificarEliminado(id);
            return Result.Success<User, DomainError>(borrado.Value);
        }
        catch (ApiException ex) {
            _logger.Error(ex, "Error de API al eliminar el usuario {Id}", id);
            return Result.Failure<User, DomainError>(
                new DomainError.ApiError((int)ex.StatusCode, ex.Message));
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error inesperado al eliminar el usuario {Id}", id);
            return Result.Failure<User, DomainError>(new DomainError.DatabaseError(ex.Message));
        }
    }

    /// <summary>
    ///     Serializa los usuarios a JSON con <see cref="JsonSerializer"/> y escribe
    ///     el fichero en el sistema de archivos local.
    /// </summary>
    /// <returns>Ruta absoluta del fichero generado o un error.</returns>
    /// <remarks>
    ///     La llamada pública entra por <see cref="ExportAsync"/>; los usuarios se obtienen
    ///     mediante <see cref="GetAllAsync"/> para conservar el flujo local/API del servicio.
    ///     El fichero se escribe en <see cref="DatabaseConfig.UsersJsonPath"/>, de modo que
    ///     el consumidor y el servicio apuntan siempre al mismo sitio.
    /// </remarks>
    private async Task<Result<string, DomainError>> ExportToJsonAsync() {
        try {
            var usuarios = await GetAllAsync();

            var json = JsonSerializer.Serialize(usuarios, new JsonSerializerOptions {
                WriteIndented = true
            });

            var ruta = DatabaseConfig.UsersJsonPath;
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            await File.WriteAllTextAsync(ruta, json);

            return Result.Success<string, DomainError>(ruta);
        }
        catch (Exception ex) {
            _logger.Error(ex, "Error al exportar los usuarios a JSON");
            return Result.Failure<string, DomainError>(new DomainError.DatabaseError(ex.Message));
        }
    }

    /// <summary>
    ///     Comprueba si un usuario existe sin crear registros como efecto secundario.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>El usuario encontrado o un <see cref="DomainError.NotFound"/>.</returns>
    /// <remarks>
    ///     A diferencia de <see cref="GetByIdAsync"/>, este método no persiste nada.
    ///     Si lo hiciera, PUT y DELETE sobre un id inexistente lo crearían primero y
    ///     nunca devolverían el 404 que exige el enunciado.
    /// </remarks>
    private async Task<Result<User, DomainError>> ComprobarExistenciaAsync(int id) {
        var local = await repository.GetByIdAsync(id);
        if (local.IsSuccess) return local;

        try {
            var remoto = await api.GetUsersByIdAsync(id);
            if (remoto is null) {
                return Result.Failure<User, DomainError>(new DomainError.NotFound("User", id));
            }

            return Result.Success<User, DomainError>(remoto);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return Result.Failure<User, DomainError>(new DomainError.NotFound("User", id));
        }
    }

    /// <summary>
    ///     Normaliza un usuario devuelto por la API REST antes de persistirlo.
    /// </summary>
    /// <remarks>
    ///     La API no devuelve los campos de auditoría, por lo que Refit los deja en
    ///     <c>default</c>. Sin esta normalización la base de datos local guardaría
    ///     <c>0001-01-01</c> en <c>CreateAt</c>.
    /// </remarks>
    /// <param name="usuario">Usuario tal y como lo devuelve la API.</param>
    /// <returns>El mismo usuario con los campos de auditoría inicializados.</returns>
    private static User DesdeApi(User usuario) => usuario with {
        CreateAt = DateTime.UtcNow,
        UpdateAt = default,
        DeleteAt = default,
        IsDeleted = false
    };

    /// <summary>
    ///     Devuelve la clave de caché asociada al usuario con el id indicado.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Clave de caché.</returns>
    private static string GetKeyUser(int id) => $"User:{id}";
}
