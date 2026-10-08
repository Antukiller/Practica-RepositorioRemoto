using System.Linq;
using System.Reactive.Linq;
using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Practicas_RepositorioRemoto.Api;
using Practicas_RepositorioRemoto.Cache.Common;
using Practicas_RepositorioRemoto.Config;
using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Entity;
using Practicas_RepositorioRemoto.Enum;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Infrastructure;
using Practicas_RepositorioRemoto.Mapper;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Notifications;
using Practicas_RepositorioRemoto.Repository;
using Practicas_RepositorioRemoto.Services;
using Practicas_RepositorioRemoto.Services.Background;
using Serilog;

namespace Practicas_RepositorioRemoto;

/// <summary>
///     Banco de pruebas de integración contra JSONPlaceholder.
///     El objetivo es comprobar que los tres niveles de almacenamiento
///     (caché, BD local y API REST) se mantienen coherentes.
/// </summary>
public class Program {

    private const string Titulo = "Sistema de Repositorio Remoto - Pruebas de Integración";
    private const int NumeroPruebas = 20;
    private const int DuracionSegundos = 65;

    private sealed record ResultadoPrueba(int Numero, string Nombre, bool Exito, int Aciertos, int Fallos);

    private static readonly List<ResultadoPrueba> Resultados = [];
    private static readonly List<Notification> NotificacionesRecibidas = [];

    private static int AciertosTotales;
    private static int FallosTotales;

    public static async Task Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = Titulo;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        var argumentos = args.Where(a => !a.StartsWith('-')).ToArray();
        DatabaseConfig.Init(argumentos);

        var provider = DependenciesProvider.BuildServiceProvider(DatabaseConfig.Config);

        await InitializeDatabaseAsync(provider, DatabaseConfig.RepositoryName);

        var notificaciones = provider.GetRequiredService<INotificationService>();
        notificaciones.Observable.Subscribe(notificacion => {
            NotificacionesRecibidas.Add(notificacion);
            MostrarNotificacion(notificacion);
        });

        MostrarCabecera();

        await EjecutarPruebasAsync(provider);

        MostrarResumenFinal();

        Log.CloseAndFlush();
    }

    /// <summary>
    ///     Crea la base de datos local si no existe.
    /// </summary>
    private static async Task InitializeDatabaseAsync(IServiceProvider provider, string repositoryName) {
        using var scope = provider.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        if (repositoryName.Equals("SQLite", StringComparison.OrdinalIgnoreCase)) {
            await serviceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
            return;
        }

        await serviceProvider.GetRequiredService<AppDbContextPostgre>().Database.EnsureCreatedAsync();
    }

    /// <summary>
    ///     Ejecuta el banco de pruebas completo.
    /// </summary>
    private static async Task EjecutarPruebasAsync(IServiceProvider provider) {
        Resultados.Clear();
        AciertosTotales = 0;
        FallosTotales = 0;

        Console.WriteLine();
        Console.WriteLine("=== EJECUCIÓN DE PRUEBAS ===");

        await PrepararDatosAsync(provider);

        await EjecutarPruebaAsync(provider, 1, "Carga inicial", ProbarCargaInicialAsync);
        await EjecutarPruebaAsync(provider, 2, "Recuperar todos", ProbarGetAllAsync);
        await EjecutarPruebaAsync(provider, 3, "Contar usuarios", ProbarCountAllAsync);
        await EjecutarPruebaAsync(provider, 4, "Buscar por id existente", ProbarGetByIdAsync);
        await EjecutarPruebaAsync(provider, 5, "Buscar id sin caché", ProbarGetByIdSinCacheAsync);
        await EjecutarPruebaAsync(provider, 6, "Devolución desde caché", ProbarDevolucionDesdeCacheAsync);
        await EjecutarPruebaAsync(provider, 7, "Buscar por id inexistente", ProbarGetByIdInexistenteAsync);
        await EjecutarPruebaAsync(provider, 8, "Crear usuario válido", ProbarCreateValidoAsync);
        await EjecutarPruebaAsync(provider, 9, "Crear usuario con datos inválidos", ProbarCreateInvalidosAsync);
        await EjecutarPruebaAsync(provider, 10, "Crear usuario nulo", ProbarCreateNuloAsync);
        await EjecutarPruebaAsync(provider, 11, "Actualizar usuario", ProbarUpdateAsync);
        await EjecutarPruebaAsync(provider, 12, "Actualizar id no coincide", ProbarUpdateIdNoCoincideAsync);
        await EjecutarPruebaAsync(provider, 13, "Actualizar usuario inexistente", ProbarUpdateInexistenteAsync);
        await EjecutarPruebaAsync(provider, 14, "Actualizar datos inválidos", ProbarUpdateInvalidosAsync);
        await EjecutarPruebaAsync(provider, 15, "Actualizar usuario nulo", ProbarUpdateNuloAsync);
        await EjecutarPruebaAsync(provider, 16, "Eliminar usuario", ProbarDeleteAsync);
        await EjecutarPruebaAsync(provider, 17, "Eliminar usuario inexistente", ProbarDeleteInexistenteAsync);
        await EjecutarPruebaAsync(provider, 18, "Exportar a JSON", ProbarExportAsync);
        await EjecutarPruebaAsync(provider, 19, "Comprobar notificaciones", ProbarNotificacionesAsync);
        await EjecutarPruebaAsync(provider, 20, "Servicio en segundo plano", ProbarBackgroundServiceAsync);
    }

    /// <summary>
    ///     Vuelca los usuarios de la API a la base de datos local.
    /// </summary>
    private static async Task PrepararDatosAsync(IServiceProvider provider) {
        Console.WriteLine();
        Console.WriteLine("Preparando datos iniciales...");

        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var api = scope.ServiceProvider.GetRequiredService<IJsonPlaceHolder>();

        await repository.DeleteAllAsync();

        var usuarios = await api.GetUsersAsync();

        foreach (var usuario in usuarios) {
            var resultado = await repository.CreateAsync(Normalizar(usuario));
            if (resultado.IsFailure) {
                Console.WriteLine($"  Error al cargar el usuario {usuario.Id}: {Mensaje(resultado.Error)}");
            }
        }

        Console.WriteLine($"  {usuarios.Count} usuarios cargados en la BD local.");
    }

    /// <summary>
    ///     Normaliza un usuario devuelto por la API antes de persistirlo.
    /// </summary>
    private static User Normalizar(User usuario) => usuario with {
        CreateAt = DateTime.UtcNow,
        UpdateAt = default,
        DeleteAt = default,
        IsDeleted = false
    };

    // ─────────────────────────── PRUEBAS ───────────────────────────

    /// <summary>Prueba 1: la BD local se rellena desde la API.</summary>
    private static async Task ProbarCargaInicialAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var cargados = await repository.GetAllAsync();
        Comprobar(cargados.Any(), "Se cargaron usuarios desde la API");

        var remotos = await us.GetAllAsync();
        Comprobar(remotos.Any(), "La API devuelve usuarios");
        Comprobar(remotos.Count() == cargados.Count(), "Coinciden los usuarios locales y remotos");

        Console.WriteLine("  Resumen método GetAll: Se obtuvieron {0} usuarios.", remotos.Count());
    }

    /// <summary>Prueba 2: recuperar todos los usuarios.</summary>
    private static async Task ProbarGetAllAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var usuarios = await repository.GetAllAsync();
        Comprobar(usuarios.Any(), "Se recuperaron usuarios");
        Comprobar(usuarios.Count() == 10, "Hay 10 usuarios");

        Console.WriteLine("  Listado de usuarios desde BD local:");
        MostrarUsuarios(usuarios);
    }

    /// <summary>Prueba 3: el conteo no se duplica al recuperar desde el servicio.</summary>
    private static async Task ProbarCountAllAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var locales = await repository.GetAllAsync();
        var usuarios = await us.GetAllAsync();

        Comprobar(usuarios.Count() == 10, "El servicio devuelve 10 usuarios sin duplicar");
        Comprobar(locales.Count() == usuarios.Count(), "La BD local coincide con el total");
    }

    /// <summary>Prueba 4: buscar por id existente.</summary>
    private static async Task ProbarGetByIdAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.GetByIdAsync(1);
        Comprobar(result.IsSuccess, "El usuario existe");
        Comprobar(result.Value.Id == 1, "El usuario tiene el id correcto");

        var local = await repository.GetByIdAsync(1);
        Comprobar(local.IsSuccess, "El usuario está en la BD local");

        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        var cacheado = await cache.GetAsync<User>("User:1");
        Comprobar(cacheado is not null, "El usuario se guardó en caché");

        MostrarUsuario(result.Value);
    }

    /// <summary>Prueba 5: lectura desde BD local cuando la caché está vacía.</summary>
    private static async Task ProbarGetByIdSinCacheAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var local = await repository.GetByIdAsync(2);
        Comprobar(local.IsSuccess, "El usuario 2 está en la BD local");

        var result = await us.GetByIdAsync(2);
        Comprobar(result.IsSuccess, "Se obtuvo el usuario 2 desde BD/API");

        MostrarUsuario(result.Value);
    }

    /// <summary>Prueba 6: devolución desde caché sin tocar BD ni API.</summary>
    private static async Task ProbarDevolucionDesdeCacheAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var usuario = (await repository.GetByIdAsync(1)).Value;
        var usuarioCache = usuario with { Name = "Usuario Desde Cache" };

        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        await cache.SetAsync("User:1", usuarioCache);

        var resultado = await us.GetByIdAsync(1);
        Comprobar(resultado.IsSuccess, "Se devolvió el usuario");
        Comprobar(resultado.Value.Name == "Usuario Desde Cache", "Se devolvió el valor desde caché");
    }

    /// <summary>Prueba 7: buscar un id que no existe.</summary>
    private static async Task ProbarGetByIdInexistenteAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.GetByIdAsync(9999);
        Comprobar(result.IsFailure, "El usuario no existe");
        MostrarResultado(result);
    }

    /// <summary>Prueba 8: crear un usuario válido.</summary>
    private static async Task ProbarCreateValidoAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var antes = NotificacionesRecibidas.Count;

        var result = await us.CreateAsync(CrearCreateRequestValido());
        Comprobar(result.IsSuccess, "El usuario se creó correctamente");
        Comprobar(result.Value.Id > 0, "El usuario tiene un id positivo");

        MostrarUsuario(result.Value);

        var local = await repository.GetByIdAsync(result.Value.Id);
        Comprobar(local.IsSuccess, "El usuario se guardó en la BD local");

        var nuevas = NotificacionesRecibidas.Skip(antes).ToArray();
        Comprobar(nuevas.Length == 1 && nuevas[0].Tipo == TypeNotification.Creado, "Se emitió una notificación de creación");
        Comprobar(nuevas[0].Mensaje == $"Usuario creado: {result.Value.Id}", "La notificación contiene el id correcto");
    }

    /// <summary>Prueba 9: crear con datos inválidos.</summary>
    private static async Task ProbarCreateInvalidosAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var antes = NotificacionesRecibidas.Count;
        var valido = CrearCreateRequestValido();

        await ComprobarCreateFailureAsync(us, valido with { Name = "" }, "Nombre vacío");
        await ComprobarCreateFailureAsync(us, valido with { UserName = "" }, "Username vacío");
        await ComprobarCreateFailureAsync(us, valido with { UserName = "user name" }, "Username con formato inválido");
        await ComprobarCreateFailureAsync(us, valido with { Email = "" }, "Email vacío");
        await ComprobarCreateFailureAsync(us, valido with { Email = "correo-invalido" }, "Email inválido");
        await ComprobarCreateFailureAsync(us, valido with { Phone = "" }, "Teléfono vacío");
        await ComprobarCreateFailureAsync(us, valido with { Phone = "telefono" }, "Teléfono inválido");
        await ComprobarCreateFailureAsync(us, valido with { Website = "" }, "Website vacío");
        await ComprobarCreateFailureAsync(us, valido with { Website = "web" }, "Website inválido");

        await ComprobarCreateFailureAsync(us, valido with {
            Address = new AddressDto("", "Apt 556", "Gwenborough", "92998-3874", new GeoDto("-37.3159", "81.1496"))
        }, "Dirección vacía");

        await ComprobarCreateFailureAsync(us, valido with {
            Address = new AddressDto("Kulas Light", "Apt 556", "Gwenborough", "", new GeoDto("-37.3159", "81.1496"))
        }, "Código postal vacío");

        await ComprobarCreateFailureAsync(us, valido with {
            Address = new AddressDto("Kulas Light", "Apt 556", "Gwenborough", "92998-3874", new GeoDto("latitud", "longitud"))
        }, "Geolocalización inválida");

        await ComprobarCreateFailureAsync(us, valido with {
            Company = new CompanyDto("", "A company", "bs")
        }, "Compañía vacía");

        Comprobar(NotificacionesRecibidas.Count == antes, "No se emitió ninguna notificación con datos inválidos");
    }

    /// <summary>Prueba 10: crear con petición nula.</summary>
    private static async Task ProbarCreateNuloAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.CreateAsync(null!);
        Comprobar(result.IsFailure, "Se rechaza una petición nula");
        MostrarResultado(result);
    }

    /// <summary>Prueba 11: actualizar un usuario y verificar la invalidación de caché.</summary>
    private static async Task ProbarUpdateAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();

        await us.GetByIdAsync(1);
        var cacheado = await cache.GetAsync<User>("User:1");
        Comprobar(cacheado is not null, "El usuario 1 está en caché antes del update");

        var antes = NotificacionesRecibidas.Count;

        var result = await us.UpdateAsync(1, CrearUpdateRequestValido());
        Comprobar(result.IsSuccess, "El usuario se actualizó correctamente");
        Comprobar(result.Value.Id == 1, "El usuario actualizado tiene el id correcto");

        MostrarUsuario(result.Value);

        var local = await repository.GetByIdAsync(1);
        Comprobar(local.IsSuccess && local.Value.Name == "Usuario Actualizado", "La BD local refleja el cambio");

        var cacheTrasUpdate = await cache.GetAsync<User>("User:1");
        Comprobar(cacheTrasUpdate is null, "La caché se invalida tras el update");

        var nuevas = NotificacionesRecibidas.Skip(antes).ToArray();
        Comprobar(nuevas.Length == 1 && nuevas[0].Tipo == TypeNotification.Actualizado, "Se emitió una notificación de actualización");
        Comprobar(nuevas[0].Mensaje == "Usuario actualizado: 1", "La notificación indica el id actualizado");
    }

    /// <summary>Prueba 12: actualizar con id que no coincide.</summary>
    private static async Task ProbarUpdateIdNoCoincideAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.UpdateAsync(1, CrearUpdateRequestValido() with { Id = 2 });
        Comprobar(result.IsFailure, "Se rechaza cuando el id de la ruta no coincide con el del cuerpo");
        MostrarResultado(result);
    }

    /// <summary>Prueba 13: actualizar un usuario inexistente.</summary>
    private static async Task ProbarUpdateInexistenteAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.UpdateAsync(9999, CrearUpdateRequestValido() with { Id = 9999 });
        Comprobar(result.IsFailure, "Se rechaza la actualización de un usuario inexistente");
        MostrarResultado(result);
    }

    /// <summary>Prueba 14: actualizar con datos inválidos.</summary>
    private static async Task ProbarUpdateInvalidosAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var antes = NotificacionesRecibidas.Count;

        var result = await us.UpdateAsync(1, CrearUpdateRequestValido() with { Email = "correo-invalido" });
        Comprobar(result.IsFailure, "Se rechaza una actualización con datos inválidos");
        MostrarResultado(result);
        Comprobar(NotificacionesRecibidas.Count == antes, "No se emitió ninguna notificación");
    }

    /// <summary>Prueba 15: actualizar con petición nula.</summary>
    private static async Task ProbarUpdateNuloAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.UpdateAsync(1, null!);
        Comprobar(result.IsFailure, "Se rechaza una actualización nula");
        MostrarResultado(result);
    }

    /// <summary>Prueba 16: eliminar un usuario.</summary>
    private static async Task ProbarDeleteAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();

        await us.GetByIdAsync(2);
        var cacheado = await cache.GetAsync<User>("User:2");
        Comprobar(cacheado is not null, "El usuario 2 está en caché antes del delete");

        var antes = NotificacionesRecibidas.Count;

        var result = await us.DeleteAsync(2);
        Comprobar(result.IsSuccess, "El usuario se eliminó correctamente");
        Comprobar(result.Value.Id == 2, "El usuario eliminado tiene el id correcto");

        MostrarUsuario(result.Value);

        var local = await repository.GetByIdAsync(2);
        Comprobar(local.IsFailure || local.Value.IsDeleted, "El usuario se marca como eliminado en la BD");

        var cacheTrasDelete = await cache.GetAsync<User>("User:2");
        Comprobar(cacheTrasDelete is null, "La caché se invalida tras el delete");

        var nuevas = NotificacionesRecibidas.Skip(antes).ToArray();
        Comprobar(nuevas.Length == 1 && nuevas[0].Tipo == TypeNotification.Eliminado, "Se emitió una notificación de eliminación");
        Comprobar(nuevas[0].Mensaje == "Usuario eliminado: 2", "La notificación indica el id eliminado");
    }

    /// <summary>Prueba 17: eliminar un usuario inexistente.</summary>
    private static async Task ProbarDeleteInexistenteAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.DeleteAsync(9999);
        Comprobar(result.IsFailure, "Se rechaza eliminar un usuario inexistente");
        MostrarResultado(result);
    }

    /// <summary>Prueba 18: exportar los usuarios a un fichero JSON.</summary>
    private static async Task ProbarExportAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var us = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await us.ExportAsync();
        Comprobar(result.IsSuccess, "La exportación se completó");

        if (result.IsSuccess) {
            Console.WriteLine("  Ruta: {0}", result.Value);
            Console.WriteLine("  Tamaño: {0} bytes", new FileInfo(result.Value).Length);

            Comprobar(File.Exists(result.Value), "El fichero se generó en disco");
            Comprobar(result.Value == DatabaseConfig.UsersJsonPath, "La ruta coincide con la configuración");
        }
    }

    /// <summary>Prueba 19: el servicio de notificaciones registró todos los tipos.</summary>
    private static async Task ProbarNotificacionesAsync(IServiceProvider provider) {
        await Task.CompletedTask;

        var creadas = NotificacionesRecibidas.Count(n => n.Tipo == TypeNotification.Creado);
        var actualizadas = NotificacionesRecibidas.Count(n => n.Tipo == TypeNotification.Actualizado);
        var eliminadas = NotificacionesRecibidas.Count(n => n.Tipo == TypeNotification.Eliminado);

        Comprobar(creadas > 0, $"Notificaciones de creación registradas ({creadas})");
        Comprobar(actualizadas > 0, $"Notificaciones de actualización registradas ({actualizadas})");
        Comprobar(eliminadas > 0, $"Notificaciones de eliminación registradas ({eliminadas})");
    }

    /// <summary>Prueba 20: el servicio en segundo plano sincroniza los datos.</summary>
    private static async Task ProbarBackgroundServiceAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();

        Console.WriteLine();
        Console.WriteLine($"Simulando trabajo del servicio en segundo plano durante {DuracionSegundos}s...");

        var usuarioTemporal = CrearCreateRequestValido().ToModel() with { Id = 999 };
        var guardado = await repository.CreateAsync(usuarioTemporal);
        Comprobar(guardado.IsSuccess, "Se creó un usuario temporal");
        if (guardado.IsFailure) {
            MostrarResultado(guardado);
            return;
        }

        var idTemporal = guardado.Value.Id;
        MostrarUsuario(guardado.Value);

        await cache.SetAsync($"User:{idTemporal}", guardado.Value);

        var backgroundService = provider.GetRequiredService<BackgroundService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DuracionSegundos));

        try {
            await backgroundService.StartAsync(cts.Token);
        }
        catch (OperationCanceledException) {
            Console.WriteLine("  Sincronización en segundo plano finalizada por límite de tiempo.");
        }

        var detalles = await repository.GetByIdAsync(idTemporal);
        Comprobar(detalles.IsFailure || detalles.Value.IsDeleted, "El usuario temporal se eliminó de la BD");

        var cacheado = await cache.GetAsync<User>($"User:{idTemporal}");
        Comprobar(cacheado is null, "La caché se limpió durante la sincronización");

        var usuarios = await repository.GetAllAsync();
        Comprobar(usuarios.Any(), "Se recargaron los usuarios desde la API");
        Console.WriteLine("  {0} usuarios sincronizados al finalizar.", usuarios.Count());
    }

    // ─────────────────────── AYUDANTES DE PRUEBA ───────────────────────

    /// <summary>Registra el resultado de una comprobación.</summary>
    private static void Comprobar(bool condicion, string mensaje) {
        if (condicion) {
            AciertosTotales++;
            Console.WriteLine($"    ✔ {mensaje}");
        }
        else {
            FallosTotales++;
            Console.WriteLine($"    ✘ {mensaje}");
        }
    }

    /// <summary>Invoca una comprobación de creación fallida.</summary>
    private static async Task ComprobarCreateFailureAsync(IUserService us, CreateUserRequest request, string mensaje) {
        var resultado = await us.CreateAsync(request);
        Comprobar(resultado.IsFailure, mensaje);
    }

    /// <summary>Ejecuta una prueba rodeándola de control de errores y registro.</summary>
    private static async Task EjecutarPruebaAsync(IServiceProvider provider, int numero, string nombre, Func<IServiceProvider, Task> prueba) {
        var aciertosAntes = AciertosTotales;
        var fallosAntes = FallosTotales;

        Console.WriteLine();
        Console.WriteLine($"--- Prueba {numero}/{NumeroPruebas}: {nombre} ---");

        try {
            await prueba(provider);
        }
        catch (Exception ex) {
            FallosTotales++;
            Console.WriteLine($"    ✘ Excepción inesperada: {ex.Message}");
        }

        var aciertosPrueba = AciertosTotales - aciertosAntes;
        var fallosPrueba = FallosTotales - fallosAntes;

        Resultados.Add(new ResultadoPrueba(numero, nombre, fallosPrueba == 0, aciertosPrueba, fallosPrueba));
    }

    /// <summary>Muestra la cabeza del resultado de una operación.</summary>
    private static void MostrarResultado<T>(Result<T, DomainError> resultado) {
        if (resultado.IsSuccess) {
            Console.WriteLine("  El resultado fue exitoso.");
        }
        else {
            Console.WriteLine($"  Error: {Mensaje(resultado.Error)}");
        }
    }

    /// <summary>Convierte un error de dominio en texto legible.</summary>
    private static string Mensaje(DomainError error) => error switch {
        DomainError.NotFound noEncontrado => $"No se encontró {noEncontrado.resource} con id {noEncontrado.id}",
        DomainError.ValidationError validacion => validacion.errorMessage,
        DomainError.ApiError api => $"API {api.statusCode}: {api.errorMessage}",
        DomainError.DatabaseError baseDatos => baseDatos.errorMessage,
        Validation validacion => string.Join(" | ", validacion.Errors),
        _ => error.ToString() ?? "Error desconocido"
    };

    /// <summary>Muestra un usuario en una línea resumida.</summary>
    private static void MostrarUsuario(User usuario) {
        Console.WriteLine($"  - {usuario.Id}: {usuario.Name} ({usuario.UserName}) <{usuario.Email}>");
    }

    /// <summary>Muestra una lista de usuarios.</summary>
    private static void MostrarUsuarios(IEnumerable<User> usuarios) {
        foreach (var usuario in usuarios) {
            MostrarUsuario(usuario);
        }
    }

    /// <summary>Muestra una notificación emitida por el servicio.</summary>
    private static void MostrarNotificacion(Notification notificacion) {
        Console.WriteLine($"    [{notificacion.Tipo}] {notificacion.Mensaje} @ {notificacion.Timestamp:HH:mm:ss}");
    }

    /// <summary>Muestra la cabecera de la aplicación.</summary>
    private static void MostrarCabecera() {
        Console.WriteLine("┌────────────────────────────────────────────────┐");
        Console.WriteLine("│    Sistema de Repositorio Remoto - Pruebas    │");
        Console.WriteLine("│      Integración con JSONPlaceholder          │");
        Console.WriteLine("└────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine($"Repositorio: {DatabaseConfig.RepositoryName}");
        Console.WriteLine($"Caché:       {DatabaseConfig.CacheName}");
        Console.WriteLine($"API:         {DatabaseConfig.BaseUrl}");
        Console.WriteLine($"BD local:    {DatabaseConfig.DataFolder}");
        Console.WriteLine($"Tiempo de sincronización: {DatabaseConfig.CacheSincronizacion}s");
    }

    /// <summary>Muestra el resumen final de todas las pruebas.</summary>
    private static void MostrarResumenFinal() {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════");
        Console.WriteLine("          RESUMEN DE PRUEBAS");
        Console.WriteLine("════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"PRUEBAS TOTALES:          {Resultados.Count}");
        Console.WriteLine($"PRUEBAS SUPERADAS:        {Resultados.Count(r => r.Exito)}");
        Console.WriteLine($"PRUEBAS NO SUPERADAS:     {Resultados.Count(r => !r.Exito)}");
        Console.WriteLine();
        Console.WriteLine("═══ COMPROBACIONES ═══");
        Console.WriteLine($"ACIERTOS TOTALES:         {AciertosTotales}");
        Console.WriteLine($"FALLOS TOTALES:           {FallosTotales}");
        Console.WriteLine();
        Console.WriteLine("═══ DETALLE POR PRUEBA ═══");

        foreach (var resultado in Resultados) {
            var estado = resultado.Exito ? "OK" : "FALLO";
            Console.WriteLine($"  [{estado}] {resultado.Numero:D2}. {resultado.Nombre} — {resultado.Aciertos} aciertos, {resultado.Fallos} fallos");
        }
    }

    // ─────────────────────── FÁBRICAS DE DATOS ───────────────────────

    /// <summary>Crea una petición de creación con datos válidos.</summary>
    private static CreateUserRequest CrearCreateRequestValido() => new(
        "Lucia Test",
        "lucia.test",
        "lucia.test@gmail.com",
        new AddressDto("Kulas Light", "Apt 556", "Gwenborough", "92998-3874", new GeoDto("-37.3159", "81.1496")),
        "600123123",
        "luciatest.com",
        new CompanyDto("Test Company", "Software company", "Development services"));

    /// <summary>Crea una petición de actualización con datos válidos.</summary>
    private static UpdateUserRequest CrearUpdateRequestValido() => new(
        1,
        "Usuario Actualizado",
        "usuario.actualizado",
        "actualizado@gmail.com",
        new AddressDto("Victor Plains", "Suite 879", "Wisokyburgh", "90566-7771", new GeoDto("-43.9509", "-34.4618")),
        "611222333",
        "usuarioactualizado.com",
        new CompanyDto("Updated Company", "Updated catch phrase", "Updated bs"));
}