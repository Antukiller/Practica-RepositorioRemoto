using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;

namespace Practicas_RepositorioRemoto.Config;
/// <summary>
/// Clase que lee los archivos appdettings.json
/// </summary>
public static class DatabaseConfig {

    public static IConfiguration Config { get; private set; } = null!;

    public static void Init(string[] args) {
        var archivoConfiguracion = (args.FirstOrDefault() ?? "Development").ToLowerInvariant() switch {
            "development" => "appsettings.Development.json",
            "production" => "appsettings.Production.json",
            var entorno => throw new ArgumentException($"Entorno de configuración no soportado: {entorno}.", nameof(args))
        };

        Config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(
                archivoConfiguracion,
                false,
                true
            )
            .Build();
    }

    /// <summary>
    /// Obtiene el nombre de la api
    /// </summary>
    public static string ApiName => Config.GetValue<string>("ApiSettings:Name") ?? "DefaultApp";
    
    /// <summary>
    /// Obtiene el link de la api rest
    /// </summary>
    public static string BaseUrl =>
        Config.GetValue<string>("ApiSettings:BaseUrl")
        ?? "https://jsonplaceholder.typicode.com"; 
    
    /// <summary>
    /// Obtiene el nombre del tipo de repositorio que se configurara
    /// </summary>
    public static string RepositoryName => Config.GetValue<string>("Repository:Name") ?? "SQLite";
    
    /// <summary>
    /// Obtiene la cadena de conexion
    /// </summary>
    public static string DbConnection =>
        RepositoryName.Equals("SQLite", StringComparison.OrdinalIgnoreCase)
            ? $"Data Source={Path.Combine(DataFolder, "usuario.db")}"
            : Config.GetValue<string>("Repository:ConnectionString")
              ?? throw new InvalidOperationException(
                  "Falta Repository:ConnectionString en la configuración.");
    
    /// <summary>
    /// Obtiene el tipo de cachee
    /// </summary>
    public static string CacheName => Config.GetValue<string>("Cache:Name") ?? "Memory";
   
    /// <summary>
    /// Obtiene el tiempo de vida de los items de la cache
    /// </summary>
    public static int CacheTtl => Config.GetValue("Cache:TTL", 30);
   
    /// <summary>
    /// Obtiene la sincronizacion
    /// </summary>
    public static int SincronizacionSegundos => Config.GetValue("BackgroundService:Sincronizacion", 60);
    
    /// <summary>
    /// Obtiene la ruta de la carpeta data
    /// </summary>
    public static string DataFolder => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "data"
    );

    /// <summary>
    /// Obtiene la ruta del archivo JSON
    /// </summary>
    public static string UsersJsonPath => Path.Combine(
        DataFolder,
        "users.json"
    );

}