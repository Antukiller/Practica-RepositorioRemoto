using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Practicas_RepositorioRemoto.Api;
using Practicas_RepositorioRemoto.Cache;
using Practicas_RepositorioRemoto.Cache.Common;
using Practicas_RepositorioRemoto.Config;
using Practicas_RepositorioRemoto.Entity;
using Practicas_RepositorioRemoto.Interfaces;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Notifications;
using Practicas_RepositorioRemoto.Repository;
using Practicas_RepositorioRemoto.Services;
using Practicas_RepositorioRemoto.Services.Background;
using Practicas_RepositorioRemoto.Validators;
using Refit;
using StackExchange.Redis;

namespace Practicas_RepositorioRemoto.Infrastructure;

/// <summary>
/// Configuración de Inyección de Dependencias con Scrutor (escaneo automático).
/// </summary>
/// <remarks>
/// Scrutor escanea el ensamblado y registra automáticamente las clases
/// que implementen las interfaces de marcador de ciclo de vida:
///   - ITransientService → Transient
///   - IScopedService    → Scoped
///   - ISingletonService  → Singleton
///
/// REQUISITO: Cada clase de servicio/repositorio DEBE implementar:
///   1. Su interfaz de negocio (IProductoService, IProductoRepository)
///   2. Una interfaz de marcador de ciclo de vida (ITransientService, etc.)
///
/// Si una clase no implementa ninguna de estas interfaces, Scrutor la ignora.
/// </remarks>
public static class DependenciesProviderScrutor
{
    /// <summary>
    /// Construye el proveedor de servicios con escaneo automático.
    /// </summary>
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Scrutor escanea todo el ensamblado y registra automáticamente:
        // - Clases que implementen ITransientService → Transient
        // - Clases que implementen IScopedService → Scoped
        // - Clases que implementen ISingletonService → Singleton
        //
        // Cada clase DEBE tener una interfaz para ser detectada.
        // Ejemplo: ProductoService implementa IProductoService + IScopedService
        services.Scan(scan => scan
            .FromAssemblyOf<Program>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo<ISingletonService>())
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()
        );
        
        

        return services.BuildServiceProvider();
    }
}

/// <summary>
/// Configuración manual de la inyección de dependencias para el banco de pruebas.
/// </summary>
/// <remarks>
/// <para>
/// Registra explícitamente los servicios necesarios para ejecutar la aplicación
/// y sus pruebas de integración (usuarios, caché, repositorio, API y notificaciones).
/// </para>
/// <para>
/// El repositorio se elige a partir de <see cref="DatabaseConfig.RepositoryName"/>
/// (SQLite o PostgreSQL) y la caché a partir de <see cref="DatabaseConfig.CacheName"/>
/// (Memory o Redis), usando siempre los valores de la configuración.
/// </para>
/// </remarks>
public static class DependenciesProvider {

    /// <summary>
    /// Construye el proveedor de servicios con el registro manual completo.
    /// </summary>
    /// <param name="config">Configuración de la aplicación.</param>
    /// <returns>El proveedor de servicios listo para resolver dependencias.</returns>
    public static ServiceProvider BuildServiceProvider(IConfiguration config) {
        var services = new ServiceCollection();

        services.AddSingleton(config);

        RegistrarRepositorio(services);

        RegistrarCaché(services);

        services.AddRefitClient<IJsonPlaceHolder>()
            .ConfigureHttpClient(client => {
                client.BaseAddress = new Uri(DatabaseConfig.BaseUrl);
            });

        services.AddScoped<IValidador<Address>, ValidadorAddress>();
        services.AddScoped<IValidador<Company>, ValidadorCompany>();
        services.AddScoped<IValidador<User>, ValidadorUser>();

        services.AddScoped<IUserService, UsersService>();
        services.AddSingleton<INotificationService, ConsoleNotificationService>();
        services.AddSingleton<BackgroundService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Registra el contexto y el repositorio según el tipo configurado.
    /// </summary>
    private static void RegistrarRepositorio(IServiceCollection services) {
        if (DatabaseConfig.RepositoryName.StartsWith("Postgre", StringComparison.OrdinalIgnoreCase)) {
            services.AddDbContext<AppDbContextPostgre>(options =>
                options.UseNpgsql(DatabaseConfig.DbConnection));

            services.AddScoped<IUserRepository, UserRepositoryPostgre>();
            return;
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(DatabaseConfig.DbConnection));

        services.AddScoped<IUserRepository, UserRepositorySqLite>();
    }

    /// <summary>
    /// Registra la implementación de <see cref="ICache"/> según la configuración.
    /// </summary>
    private static void RegistrarCaché(IServiceCollection services) {
        if (DatabaseConfig.CacheName.Equals("Redis", StringComparison.OrdinalIgnoreCase)) {
            var conexion = DatabaseConfig.Config.GetValue<string>("Cache:ConnectionString")
                ?? "localhost:6379";

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(conexion));

            services.AddScoped<ICache, RedisCache>();
            return;
        }

        services.AddSingleton<IMemoryCache, MemoryCache>();
        services.AddScoped<ICache, MemCache>();
    }
}