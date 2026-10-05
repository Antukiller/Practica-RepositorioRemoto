namespace Practicas_RepositorioRemoto.Cache.Common;

public interface ICache
{
    /// <summary>Obtiene un valor de la caché.</summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="key">Clave del valor.</param>
    /// <returns>Valor encontrado o default.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>Guarda un valor en la caché.</summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="key">Clave.</param>
    /// <param name="value">Valor a guardar.</param>
    /// <param name="expiration">Tiempo de expiración.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>Elimina un valor de la caché.</summary>
    /// <param name="key">Clave a eliminar.</param>
    Task RemoveAsync(string key);

    /// <summary>Elimina todos los valores de la caché.</summary>
    Task RemoveAllAsync();

    /// <summary>
    /// Añade una clave al índice de claves propias. Permite recuperar las claves
    /// insertadas sin depender del formato con el que se construyeron.
    /// </summary>
    /// <param name="key">Clave a indexar.</param>
    Task AddToIndexAsync(string key);

    /// <summary>
    /// Devuelve las claves propias que están indexadas.
    /// </summary>
    /// <returns>Las claves indexadas, o una colección vacía si no hay ninguna.</returns>
    Task<IReadOnlyCollection<string>> GetIndexedKeysAsync();
}