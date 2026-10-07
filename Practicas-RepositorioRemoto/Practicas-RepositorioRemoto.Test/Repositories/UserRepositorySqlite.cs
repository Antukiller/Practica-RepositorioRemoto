using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Practicas_RepositorioRemoto.Entity;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Repository;

namespace Practicas_RepositorioRemoto.Test.Repositories;

[TestFixture]
public class UserRepositorySqlite {
    private SqliteConnection _connection = null!;

    [SetUp]
    public void Setup() {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new UserRepositorySqLite(_context);        }
    private AppDbContext _context = null!;
    private UserRepositorySqLite _repository = null!;
    [TearDown]
    public void TearDown() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
    [TestFixture]
    public class CasosPositivos : UserRepositorySqlite {
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
        [Test]
        public async Task Create_UsuarioValido_DeberiaCrearConIdGenerado() {
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
        public async Task GetAll_ConDatos_DeberiaRetornarTodos() {
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
        public async Task GetById_Existente_DeberiaRetornarUsuario() {
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
        public async Task GetById_DeberiaPersistirAddressYCompany() {
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
        public async Task Update_Existente_DeberiaPersistirEnBaseDeDatos() {
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
        public async Task Delete_Existente_DeberiaMarcarComoEliminado() {
            // Arrange
            var creado = await _repository.CreateAsync(CrearUsuarioValido());

            // Act
            var resultado = await _repository.DeleteAsync(creado.Value.Id);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.IsDeleted.Should().BeTrue();
            resultado.Value.DeleteAt.Should().NotBe(default);

            var enDb = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == creado.Value.Id);
            enDb.Should().NotBeNull();
            enDb!.IsDeleted.Should().BeTrue();
        }

        [TestFixture]
        public class CasosNegativos : UserRepositorySqlite {
            [Test]
            public async Task GetById_Inexistente_DeberiaRetornarNotFound() {
                // Arrange & Act
                var resultado = await _repository.GetByIdAsync(9999);

                // Assert
                resultado.IsFailure.Should().BeTrue();
                resultado.Error.Should().BeOfType<DomainError.NotFound>();
            }
            [Test]
            public async Task Update_Inexistente_DeberiaRetornarNotFound() {
                // Arrange & Act
                var resultado = await _repository.UpdateAsync(9999, CrearUsuarioValido(9999));

                // Assert
                resultado.IsFailure.Should().BeTrue();
                resultado.Error.Should().BeOfType<DomainError.NotFound>();
            }
            [Test]
            public async Task Update_UsuarioYaEliminado_DeberiaRetornarDatabaseError() {
                var usuario = await _repository.CreateAsync(CrearUsuarioValido());
                await _repository.DeleteAsync(usuario.Value.Id);

                var datosActualizados = usuario.Value with { Name = "Nuevo Nombre" };

                // Act
                var resultado = await _repository.UpdateAsync(usuario.Value.Id, datosActualizados);

                // Assert
                resultado.IsFailure.Should().BeTrue();
                resultado.Error.Should().BeOfType<DomainError.DatabaseError>();
            }
            [Test]
            public async Task Create_ClaveDuplicada_DeberiaCapturarExcepcionYRetornarDatabaseError()
            {
                // Arrange: Creamos un usuario base
                var usuarioOriginal = await _repository.CreateAsync(CrearUsuarioValido());

                // Creamos otro usuario forzando el mismo ID exacto que ya existe
                var usuarioDuplicado = CrearUsuarioValido() with { Id = usuarioOriginal.Value.Id };

                // Act
                var resultado = await _repository.CreateAsync(usuarioDuplicado);

                // Assert
                resultado.IsFailure.Should().BeTrue();
                resultado.Error.Should().BeOfType<DomainError.DatabaseError>();
            }
        }
    }
}