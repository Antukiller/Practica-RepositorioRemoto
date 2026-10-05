using FluentAssertions;
using Practicas_RepositorioRemoto.Cache;
using Practicas_RepositorioRemoto.Models;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Practicas_RepositorioRemoto.Test.Cache;

/// <summary>
/// Tests de integración de RedisCache usando TestContainers.
/// Cada suite levanta un Redis efímero con Docker.
/// </summary>
/// <remarks>
/// Estos tests cubren el contrato de <see cref="ICache"/>, incluido el índice de claves.
/// Comprueban además que RemoveAllAsync solo toca las claves propias de la aplicación.
/// </remarks>
[TestFixture]
public class RedisCacheTests
{
    private RedisContainer _container = null!;
    private IConnectionMultiplexer _mux = null!;
    private RedisCache _cache = null!;

    /// <summary>
    /// Levanta un Redis efímero compartido por todos los tests de la suite.
    /// </summary>
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new RedisBuilder("redis:7-alpine").Build();
        await _container.StartAsync();

        // Sin allowAdmin: es la configuración que usa la aplicación por defecto.
        _mux = await ConnectionMultiplexer.ConnectAsync(
            $"{_container.Hostname}:{_container.GetMappedPublicPort(6379)},abortConnect=false");

        _cache = new RedisCache(_mux);
    }

    /// <summary>
    /// Apaga y destruye el contenedor al terminar la suite.
    /// </summary>
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _mux.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Deja la base de datos limpia antes de cada test.
    /// </summary>
    [SetUp]
    public async Task SetUp()
    {
        await _cache.RemoveAllAsync();
        await _mux.GetDatabase().KeyDeleteAsync("clave:ajena");
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
    public async Task SetAsync_GetAsync_DebeConservarElObjetoCompleto()
    {
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
    public async Task GetAsync_ClaveInexistente_DebeDevolverNull()
    {
        // Arrange & Act
        var leido = await _cache.GetAsync<User>("users:999");

        // Assert
        leido.Should().BeNull();
    }

    [Test]
    public async Task AddToIndexAsync_GetIndexedKeysAsync_DebeDevolverLasClaves()
    {
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
    public async Task AddToIndexAsync_ClaveRepetida_NoDebeDuplicar()
    {
        // Arrange
        await _cache.AddToIndexAsync("users:1");

        // Act
        await _cache.AddToIndexAsync("users:1");
        var indexadas = await _cache.GetIndexedKeysAsync();

        // Assert: un SET no admite duplicados
        indexadas.Should().ContainSingle().Which.Should().Be("users:1");
    }

    [Test]
    public async Task RemoveAsync_DebeEliminarLaClaveYSacarlaDelIndice()
    {
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
    public async Task GetIndexedKeysAsync_SinIndices_DebeDevolverVacio()
    {
        // Arrange & Act
        var indexadas = await _cache.GetIndexedKeysAsync();

        // Assert
        indexadas.Should().BeEmpty();
    }

    [Test]
    public async Task RemoveAllAsync_DebeBorrarSoloLasClavesPropias()
    {
        // Arrange
        await _cache.SetAsync("users:1", CrearUsuario(1));
        await _cache.SetAsync("users:2", CrearUsuario(2));
        await _cache.AddToIndexAsync("users:1");
        await _mux.GetDatabase().StringSetAsync("clave:ajena", "no soy mio");

        // Act
        await _cache.RemoveAllAsync();

        // Assert
        (await _cache.GetAsync<User>("users:1")).Should().BeNull();
        (await _cache.GetAsync<User>("users:2")).Should().BeNull();
        (await _mux.GetDatabase().StringGetAsync("clave:ajena")).ToString()
            .Should().Be("no soy mio");
    }

    [Test]
    public async Task RemoveAllAsync_DebeLimpiarTambienElIndice()
    {
        // Arrange
        await _cache.AddToIndexAsync("users:1");

        // Act
        await _cache.RemoveAllAsync();

        // Assert
        (await _cache.GetIndexedKeysAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task RemoveAllAsync_SinClavesPropias_NoDebeFallar()
    {
        // Arrange & Act & Assert
        await _cache.RemoveAllAsync();
        await _cache.RemoveAllAsync();
    }
}