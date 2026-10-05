using System.Text.Json;
using Practicas_RepositorioRemoto.Cache.Common;
using Practicas_RepositorioRemoto.Interfaces;
using Serilog;
using StackExchange.Redis;

namespace Practicas_RepositorioRemoto.Cache;

/// <summary>
/// Implementación de <see cref="ICache"/> respaldada por Redis.
/// </summary>
/// <param name="redis">Multiplexor de conexiones a Redis</param>
/// <param name="keyPrefix">Prefijo que identifica las claves propias de la aplicación</param>
/// <remarks>
/// Todas las operaciones son "fail-soft": si Redis falla se registra el error y se
/// devuelve el valor por defecto, porque la caché no debe provocar un fallo de la API.
/// </remarks>
public class RedisCache(IConnectionMultiplexer redis, string keyPrefix = "users:") : ICache, IScopedService
{
    private readonly ILogger _logger = Log.ForContext<RedisCache>();

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNull)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error obteniendo de caché Redis. Clave={Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var db = redis.GetDatabase();
            var serializedValue = JsonSerializer.Serialize(value);
            var ttl = expiration ?? TimeSpan.FromMinutes(5);

            await db.StringSetAsync(key, serializedValue, ttl);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error estableciendo caché Redis. Clave={Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error eliminando de caché Redis. Clave={Key}", key);
        }
    }

    /// <summary>
    /// Elimina las claves propias de la aplicación.
    /// </summary>
    /// <remarks>
    /// No se usa FLUSHDB porque es un comando de tipo admin: exige <c>allowAdmin=true</c>
    /// en la cadena de conexión y borraría la base de datos entera de Redis, incluidos
    /// datos ajenos a esta aplicación. Las claves propias se localizan por prefijo con SCAN.
    /// </remarks>
    public async Task RemoveAllAsync()
    {
        try
        {
            var db = redis.GetDatabase();

            foreach (var endpoint in redis.GetEndPoints())
            {
                var server = redis.GetServer(endpoint);
                if (server.IsReplica)
                {
                    continue;
                }

                var keys = server.Keys(pattern: $"{keyPrefix}*").ToList();
                if (keys.Count == 0)
                {
                    _logger.Debug("No hay claves propias que limpiar en Redis. Prefijo={Prefijo}", keyPrefix);
                    continue;
                }

                await db.KeyDeleteAsync(keys.ToArray());
                _logger.Debug("Se han limpiado {Cantidad} claves de Redis. Prefijo={Prefijo}", keys.Count, keyPrefix);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error limpiando la caché en Redis.");
        }
    }
}