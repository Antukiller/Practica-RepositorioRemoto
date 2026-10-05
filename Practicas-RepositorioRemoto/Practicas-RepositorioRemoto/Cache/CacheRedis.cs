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
public class RedisCache(IConnectionMultiplexer redis, string keyPrefix = "users:") : ICache, IScopedService
{
    private readonly ILogger _logger = Log.ForContext<RedisCache>();

    /// <summary>
    /// Clave del conjunto (SET) donde se indexan las claves propias.
    /// </summary>
    private string IndexKey => GetPrefixedKey("ids");

    /// <summary>
    /// Formatea la clave para asegurar que siempre incluya el prefijo.
    /// </summary>
    private string GetPrefixedKey(string key) => key.StartsWith(keyPrefix) ? key : $"{keyPrefix}{key}";

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var db = redis.GetDatabase();
            var fullKey = GetPrefixedKey(key);
            var value = await db.StringGetAsync(fullKey);

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
            var fullKey = GetPrefixedKey(key);
            var serializedValue = JsonSerializer.Serialize(value);
            var ttl = expiration ?? TimeSpan.FromMinutes(5);

            await db.StringSetAsync(fullKey, serializedValue, ttl);
            await AddToIndexAsync(fullKey); // Indexamos la clave automáticamente
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error estableciendo caché Redis. Clave={Key}", key);
        }
    }

    /// <summary>
    /// Elimina una clave de la caché y la retira del índice.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            var db = redis.GetDatabase();
            var fullKey = GetPrefixedKey(key);
            await db.KeyDeleteAsync(fullKey);
            await db.SetRemoveAsync(IndexKey, fullKey);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error eliminando de caché Redis. Clave={Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task AddToIndexAsync(string key)
    {
        try
        {
            var db = redis.GetDatabase();
            var fullKey = GetPrefixedKey(key);
            await db.SetAddAsync(IndexKey, fullKey);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error indexando la clave en Redis. Clave={Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetIndexedKeysAsync()
    {
        try
        {
            var db = redis.GetDatabase();
            var members = await db.SetMembersAsync(IndexKey);
            return members.Select(member => member.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error leyendo el índice de claves de Redis.");
            return [];
        }
    }

    /// <summary>
    /// Elimina de forma eficiente las claves indexadas de la aplicación sin bloquear Redis.
    /// </summary>
    public async Task RemoveAllAsync()
    {
        try
        {
            var db = redis.GetDatabase();
            
            // 1. Obtenemos las claves registradas en nuestro propio índice
            var indexedKeys = await GetIndexedKeysAsync();

            if (indexedKeys.Count == 0)
            {
                _logger.Debug("No hay claves propias que limpiar en Redis. Prefijo={Prefijo}", keyPrefix);
                return;
            }

            // 2. Preparamos las claves a eliminar junto con la propia clave del índice
            var keysToDelete = indexedKeys
                .Select(k => (RedisKey)k)
                .Append((RedisKey)IndexKey)
                .ToArray();

            // 3. Borramos todas de una sola llamada
            await db.KeyDeleteAsync(keysToDelete);
            _logger.Debug("Se han limpiado {Cantidad} claves de Redis. Prefijo={Prefijo}", keysToDelete.Length, keyPrefix);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error limpiando la caché en Redis.");
        }
    }
}