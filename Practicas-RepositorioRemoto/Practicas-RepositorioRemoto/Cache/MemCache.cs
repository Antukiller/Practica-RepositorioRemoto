using Microsoft.Extensions.Caching.Memory;
using Practicas_RepositorioRemoto.Cache.Common;
using Serilog;

namespace Practicas_RepositorioRemoto.Cache;

/// <summary>
/// Implementación de la caché en memoria usando IMemoryCache y un listado local de claves.
/// </summary>
public class MemCache(IMemoryCache cache, string keyPrefix = "users:") : ICache {
    private readonly ILogger _logger = Log.ForContext<MemCache>();
    
    private readonly List<string> _claves = new();

    /// <summary>
    /// Añade el prefijo a la clave si no lo contiene ya.
    /// </summary>
    private string GetPrefixedKey(string key) => 
        key.StartsWith(keyPrefix) ? key : $"{keyPrefix}{key}";
    
    /// <summary>
    /// Obtiene un elemento guardado en la caché a partir de su clave.
    /// </summary>
    public Task<T?> GetAsync<T>(string key) {
        try {
            var fullKey = GetPrefixedKey(key);
            var item = cache.Get<T>(fullKey);
            return Task.FromResult(item);
        } catch (Exception e) {
            _logger.Warning(e, "Error al obtener item de la cache: {key}", key);
            return Task.FromResult(default(T));
        }
    }

    /// <summary>
    /// Guarda un elemento en la caché con tiempo de expiración y registra su clave.
    /// </summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) {
        try {
            var fullKey = GetPrefixedKey(key);
            var op = new MemoryCacheEntryOptions();
            if (expiration.HasValue) {
                op.SetAbsoluteExpiration(expiration.Value);
            } else {
                op.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            }
            cache.Set(fullKey, value, op);
            if (!_claves.Contains(fullKey)) 
                _claves.Add(fullKey);
            return Task.CompletedTask;
        } catch (Exception e) {
            _logger.Warning(e, "Error al guardar item en la cache: {key}", key);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Elimina un elemento de la caché y lo quita de la lista de claves.
    /// </summary>
    public Task RemoveAsync(string key) {
        try {
            var fullKey = GetPrefixedKey(key);
            cache.Remove(fullKey);
            _claves.Remove(fullKey); 
            return Task.CompletedTask;
        } catch (Exception e) {
            _logger.Warning(e, "Error al remover item de la cache: {key}", key);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Elimina de la caché todas las claves registradas y limpia la lista.
    /// </summary>
    public Task RemoveAllAsync() {
        try {
            foreach (var k in _claves) {
                cache.Remove(k);
            }
            _claves.Clear();
            return Task.CompletedTask;
        } catch (Exception e) {
            _logger.Warning(e, "Error al borrar las claves de la cache");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Registra manualmente una clave en la lista si aún no existe.
    /// </summary>
    public Task AddToIndexAsync(string key) {
        var fullKey = GetPrefixedKey(key);
        if (!_claves.Contains(fullKey)) {
            _claves.Add(fullKey);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Devuelve la colección con todas las claves registradas.
    /// </summary>
    public Task<IReadOnlyCollection<string>> GetIndexedKeysAsync() {
        return Task.FromResult<IReadOnlyCollection<string>>(_claves);
    }
}