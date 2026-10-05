using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Practicas_RepositorioRemoto.Entity;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Repository;
using Testcontainers.PostgreSql;

namespace Practicas_RepositorioRemoto.Test.Repositories;

/// <summary>
/// Tests de integración de UserRepositoryPostgre usando TestContainers.
/// Cada suite crea un contenedor PostgreSQL efímero con Docker.
/// </summary>
/// <remarks>
/// A diferencia de los tests unitarios de los validadores, aquí no se mockea nada:
/// se levanta un PostgreSQL real para comprobar que el mapeo de EF Core, los tipos
/// propiedad (Address, Company, Geo) y las operaciones CRUD funcionan contra el motor real.
/// </remarks>
[TestFixture]
public class UserRepositoryPostgreTests
{
    private PostgreSqlContainer _container = null!;
    private AppDbContextPostgre _context = null!;
    private UserRepositoryPostgre _repository = null!;

    /// <summary>
    /// Levanta un PostgreSQL efímero que se comparte entre todos los tests de la suite.
    /// </summary>
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("test_db")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();
    }

    /// <summary>
    /// Apaga y destruye el contenedor al terminar la suite.
    /// </summary>
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Crea el esquema de la base de datos y el repositorio antes de cada test,
    /// de modo que cada uno parte de una base de datos vacía.
    /// </summary>
    [SetUp]
    public async Task SetUp()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContextPostgre>();
        optionsBuilder.UseNpgsql(_container.GetConnectionString());

        _context = new AppDbContextPostgre(optionsBuilder.Options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new UserRepositoryPostgre(_context);
    }

    /// <summary>
    /// Elimina el esquema y libera el contexto tras cada test.
    /// </summary>
    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Crea un usuario válido que sirve de base para los casos de prueba.
    /// </summary>
    /// <param name="id">Identificador; 0 deja que lo genere la base de datos</param>
    private static User CrearUsuarioValido(int id = 0) => new(
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

    /// <summary>
    /// Casos en los que el repositorio debe devolver un Result con éxito.
    /// </summary>
    [TestFixture]
    public class CasosPositivos : UserRepositoryPostgreTests
    {
        [Test]
        public async Task Create_UsuarioValido_DeberiaCrearConIdGenerado()
        {
            // Arrange
            var usuario = CrearUsuarioValido();

            // Act
            var resultado = await _repository.CreateAsync(usuario);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Id.Should().BeGreaterThan(0);
            resultado.Value.Name.Should().Be("Leanne Graham");
            resultado.Value.Email.Should().Be("bret@srav.com");
        }

        [Test]
        public async Task GetAll_ConDatos_DeberiaRetornarTodos()
        {
            // Arrange
            await _repository.CreateAsync(CrearUsuarioValido() with { Name = "Usuario A" });
            await _repository.CreateAsync(CrearUsuarioValido() with { Name = "Usuario B" });

            // Act
            var resultados = await _repository.GetAllAsync();

            // Assert
            resultados.Should().HaveCount(2);
            resultados.Select(u => u.Name).Should().BeEquivalentTo(["Usuario A", "Usuario B"]);
        }

        [Test]
        public async Task GetById_Existente_DeberiaRetornarUsuario()
        {
            // Arrange
            var creado = await _repository.CreateAsync(
                CrearUsuarioValido() with { Name = "Clementine Bauch" });

            // Act
            var encontrado = await _repository.GetByIdAsync(creado.Value.Id);

            // Assert
            encontrado.IsSuccess.Should().BeTrue();
            encontrado.Value.Name.Should().Be("Clementine Bauch");
            encontrado.Value.UserName.Should().Be("Bret");
        }

        [Test]
        public async Task GetById_DeberiaPersistirAddressYCompany()
        {
            // Arrange
            var creado = await _repository.CreateAsync(CrearUsuarioValido());

            // Act
            var encontrado = await _repository.GetByIdAsync(creado.Value.Id);

            // Assert
            encontrado.IsSuccess.Should().BeTrue();
            encontrado.Value.Address.Should().NotBeNull();
            encontrado.Value.Address.City.Should().Be("Gwenborough");
            encontrado.Value.Address.ZipCode.Should().Be("92998-3874");
            encontrado.Value.Address.Geo.Lat.Should().Be("-37.3159");
            encontrado.Value.Company.Name.Should().Be("Romaguera-Crona");
        }

        [Test]
        public async Task Update_Existente_DeberiaPersistirEnBaseDeDatos()
        {
            // Arrange
            var creado = await _repository.CreateAsync(CrearUsuarioValido());

            // Act
            var resultado = await _repository.UpdateAsync(creado.Value.Id,
                creado.Value with { Email = "nuevo@srav.com", Name = "Nombre Actualizado" });

            // Assert
            resultado.IsSuccess.Should().BeTrue();

            // Se relee desde la base de datos: comprobar solo el valor devuelto
            // no detectaría un UpdateAsync que no persista nada.
            var releido = await _repository.GetByIdAsync(creado.Value.Id);
            releido.IsSuccess.Should().BeTrue();
            releido.Value.Email.Should().Be("nuevo@srav.com");
            releido.Value.Name.Should().Be("Nombre Actualizado");
        }

        [Test]
        public async Task Delete_Existente_DeberiaMarcarComoEliminado()
        {
            // Arrange
            var creado = await _repository.CreateAsync(CrearUsuarioValido());

            // Act
            var resultado = await _repository.DeleteAsync(creado.Value.Id);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.IsDeleted.Should().BeTrue();
            resultado.Value.DeleteAt.Should().NotBe(default);

            // El borrado es lógico: la fila permanece en la tabla.
            var releido = await _repository.GetByIdAsync(creado.Value.Id);
            releido.IsSuccess.Should().BeTrue();
            releido.Value.IsDeleted.Should().BeTrue();
        }
    }

    /// <summary>
    /// Casos en los que el repositorio debe devolver un Result con error.
    /// </summary>
    [TestFixture]
    public class CasosNegativos : UserRepositoryPostgreTests
    {
        [Test]
        public async Task GetById_Inexistente_DeberiaRetornarNotFound()
        {
            // Arrange & Act
            var resultado = await _repository.GetByIdAsync(9999);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<DomainError.NotFound>();
        }

        [Test]
        public async Task Update_Inexistente_DeberiaRetornarNotFound()
        {
            // Arrange & Act
            var resultado = await _repository.UpdateAsync(9999, CrearUsuarioValido(9999));

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<DomainError.NotFound>();
        }

        [Test]
        public async Task Delete_Inexistente_DeberiaRetornarNotFound()
        {
            // Arrange & Act
            var resultado = await _repository.DeleteAsync(9999);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<DomainError.NotFound>();
        }

        [Test]
        public async Task Update_UsuarioYaEliminado_DeberiaRetornarDatabaseError()
        {
            // Arrange
            var creado = await _repository.CreateAsync(
                CrearUsuarioValido() with { IsDeleted = true, DeleteAt = DateTime.UtcNow });

            // Act
            var resultado = await _repository.UpdateAsync(creado.Value.Id, creado.Value);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<DomainError.DatabaseError>();
        }

        [Test]
        public async Task Create_NombreDemasiadoLargo_DeberiaRetornarDatabaseError()
        {
            // Arrange: la columna Name está mapeada con HasMaxLength(150)
            var usuario = CrearUsuarioValido() with { Name = new string('a', 200) };

            // Act
            var resultado = await _repository.CreateAsync(usuario);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<DomainError.DatabaseError>();
        }
    }
}