using Microsoft.Extensions.DependencyInjection;
using Practicas_RepositorioRemoto.Api;
using Practicas_RepositorioRemoto.Cache.Common;
using Practicas_RepositorioRemoto.Interfaces;
using Practicas_RepositorioRemoto.Repository;
using Serilog;

namespace Practicas_RepositorioRemoto.Services.Background;

/// <summary>
/// Servicio para la ejecución periódica de sincronización de usuarios en segundo plano.
/// </summary>
public class BackgroundService(IServiceScopeFactory scopeFactory) : ISingletonService
{
    private readonly ILogger _logger = Log.ForContext<BackgroundService>();

    /// <summary>
    /// Inicia el bucle de sincronización periódica cada 60 segundos.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar la ejecución del servicio.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                _logger.Information("Iniciando ciclo de sincronización de usuarios...");

                await Synchronize();

                _logger.Information("Sincronización completada con éxito.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error durante el ciclo de sincronización de usuarios.");
            }
        }
    }

    /// <summary>
    /// Realiza la limpieza de caché y el reemplazo masivo de la tabla de usuarios con los datos de la API.
    /// </summary>
    /// <remarks>
    /// Cada dependencia se resuelve dentro de su propio scope porque el caché y los repositorios
    /// tienen lifetime <c>Scoped</c>.
    /// </remarks>
    private async Task Synchronize()
    {
        using var scope = scopeFactory.CreateScope();
        var scoped = scope.ServiceProvider;

        var repository = scoped.GetRequiredService<IUserRepository>();
        var cache = scoped.GetRequiredService<ICache>();
        var api = scoped.GetRequiredService<IJsonPlaceHolder>();

        await cache.RemoveAllAsync();

        var borrado = await repository.DeleteAllAsync();
        if (borrado.IsFailure)
        {
            _logger.Warning("No se pudo vaciar la tabla de usuarios: {Error}", borrado.Error);
            return;
        }

        var users = await api.GetUsersAsync();

        foreach (var user in users)
        {
            var creado = await repository.CreateAsync(user);
            if (creado.IsFailure)
            {
                _logger.Warning("No se pudo insertar el usuario {Id}: {Error}", user.Id, creado.Error);
            }
        }

        _logger.Information("Se sincronizaron {Total} usuarios.", users.Count);
    }
}