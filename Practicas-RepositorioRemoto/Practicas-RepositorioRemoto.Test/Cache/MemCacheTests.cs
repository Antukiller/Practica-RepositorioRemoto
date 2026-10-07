using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Practicas_RepositorioRemoto.Cache;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Test.Cache;

/// <summary>
/// Tests de unitarios de MemCache (caché en memoria basada en IMemoryCache).
/// No necesita Docker: cada suite usa su propia instancia de MemoryCache.
/// </summary>
/// <remarks>
/// Estos tests cubren el contrato de <see cref="ICache"/>, incluido el índice de claves.
/// Comprueban además que RemoveAllAsync solo toca las claves propias de la aplicación.
/// </remarks>
[TestFixture]
public class MemCacheTests {

    private MemoryCache _imemoryCache = null!;
    private MemCache _cache = null!;

    /// <summary>
    /// Crea la caché en memoria y la instancia de MemCache de la suite.
    /// </summary>
    [OneTimeSetUp]
    public void OneTimeSetUp() {
        _imemoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new MemCache(_imemoryCache);
    }

    /// <summary>
    /// Libera la caché en memoria al terminar la suite.
    /// </summary>
    [OneTimeTearDown]
    public void OneTimeTearDown() {
        _imemoryCache.Dispose();
    }

    /// <summary>
    /// Deja la caché limpia antes de cada test.
    /// </summary>
    [SetUp]
    public async Task SetUp() {
        await _cache.RemoveAllAsync();
        _imemoryCache.Remove("clave:ajena");
    }

    /// <summary>
    /// Crea un usuario válido para usar en las pruebas.
    /// </summary>
    private static User CrearUsuario(int id = 1) => new(
        id,
        "Leanne Graham",
        "Bret",
        "bret@srav.com",
        new Address("Kulas Light", "Apt. 555", "Gwenborough", "92998-3874", new Geo("-37.3159", "81.1496")),
        "1-770-736-7631",
        "hildegard.org",
        new Company("Romaguera-Crona", "Slogan de prueba", "negocios hodie"),
        DateTime.UtcNow.AddDays(-1),
        DateTime.UtcNow,
        default,
        false);

    [Test]
    public async Task SetAsync_GetAsync_DebeConservarElObjetoCompleto() {
        // Arrange
        var usuario = CrearUsuario();

        // Act
        await _cache.SetAsync("users:1", usuario);
        var leido = await _cache.GetAsync<User>("users:1");

        // Assert
        leido.Should().NotBeNull();
        leido!.Should().Be(usuario);
        leido.Address.City.Should().Be("Gwenborough");
        leido.Address.Geo.Lat.Should().Be("-37.3159");
        leido.Company.Name.Should().Be("Romaguera-Crona");
    }

    [Test]
    public async Task GetAsync_ClaveInexistente_DebeDevolverNull() {
        // Arrange & Act
        var leido = await _cache.GetAsync<User>("users:999");

        // Assert
        leido.Should().BeNull();
    }

    [Test]
    public async Task SetAsync_ConClaveSinPrefijo_LaRegistraConElPrefijo() {
        // Arrange
        var usuario = CrearUsuario(1);

        // Act
        await _cache.SetAsync("1", usuario);

        // Assert
        var leido = await _cache.GetAsync<User>("1");
        leido.Should().NotBeNull();
        leido!.Should().Be(usuario);

        (await _cache.GetAsync<User>("users:1")).Should().Be(usuario);

        var indexadas = await _cache.GetIndexedKeysAsync();
        indexadas.Should().Contain("users:1");
    }

    [Test]
    public async Task AddToIndexAsync_GetIndexedKeysAsync_DebeDevolverLasClaves() {
        // Arrange
        await _cache.SetAsync("users:1", CrearUsuario(1));
        await _cache.SetAsync("users:2", CrearUsuario(2));

        // Act
        await _cache.AddToIndexAsync("users:1");
        await _cache.AddToIndexAsync("users:2");
        var indexadas = await _cache.GetIndexedKeysAsync();

        // Assert
        indexadas.Should().BeEquivalentTo(["users:1", "users:2"]);
    }

    [Test]
    public async Task AddToIndexAsync_ClaveRepetida_NoDebeDuplicar() {
        // Arrange
        await _cache.AddToIndexAsync("users:1");

        // Act
        await _cache.AddToIndexAsync("users:1");
        var indexadas = await _cache.GetIndexedKeysAsync();

        // Assert: la lista no admite duplicados
        indexadas.Should().ContainSingle().Which.Should().Be("users:1");
    }

    [Test]
    public async Task RemoveAsync_DebeEliminarLaClaveYSacarlaDelIndice() {
        // Arrange
        await _cache.SetAsync("users:1", CrearUsuario(1));
        await _cache.AddToIndexAsync("users:1");

        // Act
        await _cache.RemoveAsync("users:1");

        // Assert
        (await _cache.GetAsync<User>("users:1")).Should().BeNull();
        (await _cache.GetIndexedKeysAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task GetIndexedKeysAsync_SinIndices_DebeDevolverVacio() {
        // Arrange & Act
        var indexadas = await _cache.GetIndexedKeysAsync();

        // Assert
        indexadas.Should().BeEmpty();
    }

    [Test]
    public async Task RemoveAllAsync_DebeBorrarSoloLasClavesPropias() {
        // Arrange
        await _cache.SetAsync("users:1", CrearUsuario(1));
        await _cache.SetAsync("users:2", CrearUsuario(2));
        await _cache.AddToIndexAsync("users:1");
        _imemoryCache.Set("clave:ajena", "no soy mio");

        // Act
        await _cache.RemoveAllAsync();

        // Assert
        (await _cache.GetAsync<User>("users:1")).Should().BeNull();
        (await _cache.GetAsync<User>("users:2")).Should().BeNull();
        _imemoryCache.Get<string>("clave:ajena").Should().Be("no soy mio");
    }

    [Test]
    public async Task RemoveAllAsync_DebeLimpiarTambienElIndice() {
        // Arrange
        await _cache.AddToIndexAsync("users:1");

        // Act
        await _cache.RemoveAllAsync();

        // Assert
        (await _cache.GetIndexedKeysAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task RemoveAllAsync_SinClavesPropias_NoDebeFallar() {
        // Arrange & Act & Assert
        await _cache.RemoveAllAsync();
        await _cache.RemoveAllAsync();
    }
}