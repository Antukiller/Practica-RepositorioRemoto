using System.Net.Http;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Practicas_RepositorioRemoto.Api;
using Practicas_RepositorioRemoto.Cache.Common;
using Practicas_RepositorioRemoto.Config;
using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Notifications;
using Practicas_RepositorioRemoto.Repository;
using Practicas_RepositorioRemoto.Services;
using Practicas_RepositorioRemoto.Validators;

namespace Practicas_RepositorioRemoto.Test.Services;

// ─── USERS SERVICE TESTS ───────────────────────────────────────────

/// <summary>
/// Tests de UsersService. Usa mocks de repositorio, validador, caché,
/// notificaciones y API. Organizados en CasosPositivos y CasosNegativos.
/// </summary>
[TestFixture]
public class UsersServiceTest
{
    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _validadorMock = new Mock<IValidador<User>>();
        _cacheMock = new Mock<ICache>();
        _notificationMock = new Mock<INotificationService>();
        _apiMock = new Mock<IJsonPlaceHolder>();

        // El validador devuelve SIEMPRE ÉXITO por defecto salvo que el test lo cambie.
        _validadorMock.Setup(v => v.Validar(It.IsAny<User>()))
            .Returns((User u) => Result.Success<User, DomainError>(u));

        _service = new UsersService(
            _validadorMock.Object,
            _repositoryMock.Object,
            _cacheMock.Object,
            _notificationMock.Object,
            _apiMock.Object
        );
    }

    protected UsersService _service = null!;
    protected Mock<IUserRepository> _repositoryMock = null!;
    protected Mock<IValidador<User>> _validadorMock = null!;
    protected Mock<ICache> _cacheMock = null!;
    protected Mock<INotificationService> _notificationMock = null!;
    protected Mock<IJsonPlaceHolder> _apiMock = null!;

    // ─── FÁBRICAS DE DATOS ──────────────────────────────────────────

    protected static User CrearUsuarioValido(int id = 1) => new(
        id,
        "Leanne Graham",
        "Bret",
        "bret@srav.com",
        new Address("Kulas Light", "Apt. 555", "Gwenborough", "92998-3874", new Geo("-37.3159", "81.1496")),
        "600123123",
        "hildegard.org",
        new Company("Romaguera-Crona", "Slogan de prueba", "negocios hodie"),
        DateTime.UtcNow.AddDays(-1),
        DateTime.UtcNow,
        default,
        false);

    protected static CreateUserRequest CrearRequestCreateValido() => new(
        "Lucia Test",
        "lucia.test",
        "lucia.test@gmail.com",
        new AddressDto("Kulas Light", "Apt 556", "Gwenborough", "92998-3874", new GeoDto("-37.3159", "81.1496")),
        "600123123",
        "luciatest.com",
        new CompanyDto("Test Company", "Software company", "Development services"));

    protected static UpdateUserRequest CrearRequestUpdateValido() => new(
        1,
        "Usuario Actualizado",
        "usuario.actualizado",
        "actualizado@gmail.com",
        new AddressDto("Victor Plains", "Suite 879", "Wisokyburgh", "90566-7771", new GeoDto("-43.9509", "-34.4618")),
        "611222333",
        "usuarioactualizado.com",
        new CompanyDto("Updated Company", "Updated catch phrase", "Updated bs"));

    // ─── CASOS POSITIVOS ─────────────────────────────────────────────

    /// <summary>
    /// Casos positivos del servicio de usuarios.
    /// </summary>
    [TestFixture]
    public class CasosPositivos : UsersServiceTest
    {
        [Test]
        public void GetAll_ConDatosEnBd_RetornaLocalesSinLlamarApi()
        {
            // Arrange
            var locales = new List<User> { CrearUsuarioValido() };
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(locales);

            // Act
            var r = _service.GetAllAsync().GetAwaiter().GetResult();

            // Assert
            r.Should().HaveCount(1);
            r.First().Name.Should().Be("Leanne Graham");
            _apiMock.Verify(a => a.GetUsersAsync(), Times.Never);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public void GetAll_BdVacia_DescargaDeApiYPersiste()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
            var remotos = new List<User> { CrearUsuarioValido(1), CrearUsuarioValido(2) };
            _apiMock.Setup(a => a.GetUsersAsync()).ReturnsAsync(remotos);

            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => Result.Success<User, DomainError>(u));

            // Act
            var r = _service.GetAllAsync().GetAwaiter().GetResult();

            // Assert
            r.Should().HaveCount(2);
            _apiMock.Verify(a => a.GetUsersAsync(), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Exactly(2));
        }

        [Test]
        public void GetAll_BdVacia_PersisteUsuariosNormalizados()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
            var remotos = new List<User> { CrearUsuarioValido(1) };
            _apiMock.Setup(a => a.GetUsersAsync()).ReturnsAsync(remotos);

            User? persistido = null;
            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) =>
                {
                    persistido = u;
                    return Result.Success<User, DomainError>(u);
                });

            // Act
            _service.GetAllAsync().GetAwaiter().GetResult();

            // Assert: la API no trae campos de auditoría y el servicio debe normalizarlos
            persistido.Should().NotBeNull();
            persistido!.CreateAt.Should().NotBe(default);
            persistido.IsDeleted.Should().BeFalse();
            persistido.DeleteAt.Should().Be(default);
        }

        [Test]
        public void GetById_ConCache_RetornaDeCacheSinTocarRepoNiApi()
        {
            // Arrange
            var cita = CrearUsuarioValido();
            _cacheMock.Setup(c => c.GetAsync<User>("User:1")).ReturnsAsync(cita);

            // Act
            var r = _service.GetByIdAsync(1).GetAwaiter().GetResult();

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Name.Should().Be("Leanne Graham");
            _cacheMock.Verify(c => c.GetAsync<User>("User:1"), Times.Once);
            _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _apiMock.Verify(a => a.GetUsersByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void GetById_SinCache_RetornaDeRepoYLlenaCache()
        {
            // Arrange
            var cita = CrearUsuarioValido();
            _cacheMock.Setup(c => c.GetAsync<User>("User:1")).ReturnsAsync((User?)null);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Result.Success<User, DomainError>(cita));
            _cacheMock.Setup(c => c.SetAsync("User:1", cita, null)).Returns(Task.CompletedTask);

            // Act
            var r = _service.GetByIdAsync(1).GetAwaiter().GetResult();

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Name.Should().Be("Leanne Graham");
            _repositoryMock.Verify(r => r.GetByIdAsync(1), Times.Once);
            _cacheMock.Verify(c => c.SetAsync("User:1", cita, null), Times.Once);
            _apiMock.Verify(a => a.GetUsersByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void GetById_SinCacheNiRepo_RetornaDeApiYPersiste()
        {
            // Arrange
            var remoto = CrearUsuarioValido(1);
            _cacheMock.Setup(c => c.GetAsync<User>("User:1")).ReturnsAsync((User?)null);
            _repositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(Result.Failure<User, DomainError>(new DomainError.NotFound("User", 1)));
            _apiMock.Setup(a => a.GetUsersByIdAsync(1)).ReturnsAsync(remoto);
            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => Result.Success<User, DomainError>(u));

            // Act
            var r = _service.GetByIdAsync(1).GetAwaiter().GetResult();

            // Assert
            r.IsSuccess.Should().BeTrue();
            _apiMock.Verify(a => a.GetUsersByIdAsync(1), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
            _cacheMock.Verify(c => c.SetAsync("User:1", r.Value), Times.Once);
        }

        [Test]
        public void Create_RequestValido_CreaEnApiYRepoYNotifica()
        {
            // Arrange
            var usuarioCreado = CrearUsuarioValido(11);
            _apiMock.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync(usuarioCreado);

            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => Result.Success<User, DomainError>(u));

            // Act
            var r = _service.CreateAsync(CrearRequestCreateValido()).GetAwaiter().GetResult();

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Id.Should().Be(11);
            _repositoryMock.Verify(r => r.CreateAsync(It.Is<User>(u => u.Id == 11 && u.Name == "Lucia Test")), Times.Once);
            _notificationMock.Verify(n => n.NotificarCreado(11), Times.Once);
            // Patrón cache-aside: la escritura NO llena la caché
            _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<TimeSpan?>()), Times.Never);
        }

        [Test]
        public void Update_RequestValido_ActualizaInvalidaCacheYNotifica()
        {
            // Arrange
            var existente = CrearUsuarioValido(1);
            var actualizado = CrearUsuarioValido(1) with { Name = "Usuario Actualizado" };

            _repositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(Result.Success<User, DomainError>(existente));
            _apiMock.Setup(a => a.UpdateUserAsync(1, It.IsAny<UpdateUserRequest>()))
                .ReturnsAsync(actualizado);
            _repositoryMock.Setup(r => r.UpdateAsync(1, It.IsAny<User>()))
                .ReturnsAsync((int id, User u) => Result.Success<User, DomainError>(u));

            // Act
            var r = _service.UpdateAsync(1, CrearRequestUpdateValido()).GetAwaiter().GetResult();

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Name.Should().Be("Usuario Actualizado");
            _repositoryMock.Verify(r => r.GetByIdAsync(1), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(1, It.IsAny<User>()), Times.Once);
            _apiMock.Verify(a => a.UpdateUserAsync(1, It.IsAny<UpdateUserRequest>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync("User:1"), Times.Once);
            _notificationMock.Verify(n => n.NotificarActualizado(1), Times.Once);
        }

        [Test]
        public void Delete_ConUsuarioExistente_EliminaLimpiaCacheYNotifica()
        {
            // Arrange
            var existente = CrearUsuarioValido(1);
            var borrado = existente with { IsDeleted = true, DeleteAt = DateTime.UtcNow };

            _repositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(Result.Success<User, DomainError>(existente));
            _apiMock.Setup(a => a.DeleteUserAsync(1)).Returns(Task.CompletedTask);
            _repositoryMock.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(Result.Success<User, DomainError>(borrado));

            // Act
            var r = _service.DeleteAsync(1).GetAwaiter().GetResult();

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.IsDeleted.Should().BeTrue();
            _repositoryMock.Verify(r => r.GetByIdAsync(1), Times.Once);
            _apiMock.Verify(a => a.DeleteUserAsync(1), Times.Once);
            _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync("User:1"), Times.Once);
            _notificationMock.Verify(n => n.NotificarEliminado(1), Times.Once);
        }

        [Test]
        public void Export_ConDatosEnBd_EscribeFicheroYDevuelveRuta()
        {
            // Arrange
            var locales = new List<User> { CrearUsuarioValido(1) };
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(locales);

            // Act
            var r = _service.ExportAsync().GetAwaiter().GetResult();

            // Assert
            r.IsSuccess.Should().BeTrue();
            r.Value.Should().Be(DatabaseConfig.UsersJsonPath);
            File.Exists(r.Value).Should().BeTrue();
        }
    }

    // ─── CASOS NEGATIVOS ─────────────────────────────────────────────

    /// <summary>
    /// Casos negativos del servicio de usuarios.
    /// </summary>
    [TestFixture]
    public class CasosNegativos : UsersServiceTest
    {
        [Test]
        public void GetAll_BdVaciaYApiFalla_LanzaExcepcion()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
            _apiMock.Setup(a => a.GetUsersAsync()).ThrowsAsync(new HttpRequestException("API caída"));

            // Act
            var act = async () => await _service.GetAllAsync();

            // Assert
            act.Should().ThrowAsync<HttpRequestException>();
        }

        [Test]
        public void GetById_ApiDevuelveNull_RetornaNotFound()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetAsync<User>("User:8")).ReturnsAsync((User?)null);
            _repositoryMock.Setup(r => r.GetByIdAsync(8))
                .ReturnsAsync(Result.Failure<User, DomainError>(new DomainError.NotFound("User", 8)));
            _apiMock.Setup(a => a.GetUsersByIdAsync(8)).ReturnsAsync((User?)null);

            // Act
            var r = _service.GetByIdAsync(8).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.NotFound>();
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public void GetById_ApiLanzaExcepcion_RetornaDatabaseError()
        {
            // Arrange
            _cacheMock.Setup(c => c.GetAsync<User>("User:1")).ReturnsAsync((User?)null);
            _repositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(Result.Failure<User, DomainError>(new DomainError.NotFound("User", 1)));
            _apiMock.Setup(a => a.GetUsersByIdAsync(1)).ThrowsAsync(new HttpRequestException("API caída"));

            // Act
            var r = _service.GetByIdAsync(1).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.DatabaseError>();
        }

        [Test]
        public void Create_RequestNulo_RetornaValidationError()
        {
            // Act
            var r = _service.CreateAsync(null!).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.ValidationError>();
            _apiMock.Verify(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
            _notificationMock.Verify(n => n.NotificarCreado(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Create_ValidacionFallida_RetornaErrorSinGuardarNiNotificar()
        {
            // Arrange
            var error = new Validation(["El email es obligatorio y debe tener un formato válido."]);
            _validadorMock.Setup(v => v.Validar(It.IsAny<User>()))
                .Returns(Result.Failure<User, DomainError>(error));

            // Act
            var r = _service.CreateAsync(CrearRequestCreateValido()).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<Validation>();
            _apiMock.Verify(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
            _notificationMock.Verify(n => n.NotificarCreado(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Create_ApiLanzaExcepcion_RetornaDatabaseError()
        {
            // Arrange
            _apiMock.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()))
                .ThrowsAsync(new HttpRequestException("API caída"));

            // Act
            var r = _service.CreateAsync(CrearRequestCreateValido()).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.DatabaseError>();
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
            _notificationMock.Verify(n => n.NotificarCreado(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Create_RepoFallido_RetornaDatabaseErrorSinNotificar()
        {
            // Arrange
            _apiMock.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()))
                .ReturnsAsync(CrearUsuarioValido(11));
            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(Result.Failure<User, DomainError>(
                    new DomainError.DatabaseError("No se pudo insertar")));

            // Act
            var r = _service.CreateAsync(CrearRequestCreateValido()).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.DatabaseError>();
            _notificationMock.Verify(n => n.NotificarCreado(11), Times.Never);
        }

        [Test]
        public void Update_RequestNulo_RetornaValidationError()
        {
            // Act
            var r = _service.UpdateAsync(1, null!).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.ValidationError>();
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<User>()), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Update_IdNoCoincide_RetornaValidationError()
        {
            // Arrange
            var request = CrearRequestUpdateValido() with { Id = 2 };

            // Act
            var r = _service.UpdateAsync(1, request).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.ValidationError>();
            _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<User>()), Times.Never);
        }

        [Test]
        public void Update_UsuarioInexistente_RetornaNotFound()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetByIdAsync(8))
                .ReturnsAsync(Result.Failure<User, DomainError>(new DomainError.NotFound("User", 8)));
            _apiMock.Setup(a => a.GetUsersByIdAsync(8)).ReturnsAsync((User?)null);

            // Act
            var r = _service.UpdateAsync(8, CrearRequestUpdateValido() with { Id = 8 }).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.NotFound>();
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<User>()), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Update_ValidacionFallida_RetornaErrorSinGuardarNiNotificar()
        {
            // Arrange
            var existente = CrearUsuarioValido(1);
            _repositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(Result.Success<User, DomainError>(existente));

            var error = new Validation(["El email es obligatorio y debe tener un formato válido."]);
            _validadorMock.Setup(v => v.Validar(It.IsAny<User>()))
                .Returns(Result.Failure<User, DomainError>(error));

            // Act
            var r = _service.UpdateAsync(1, CrearRequestUpdateValido()).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<Validation>();
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<User>()), Times.Never);
            _apiMock.Verify(a => a.UpdateUserAsync(It.IsAny<int>(), It.IsAny<UpdateUserRequest>()), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
            _notificationMock.Verify(n => n.NotificarActualizado(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Update_RepoFallido_RetornaDatabaseErrorSinNotificar()
        {
            // Arrange
            var existente = CrearUsuarioValido(1);
            _repositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(Result.Success<User, DomainError>(existente));
            _apiMock.Setup(a => a.UpdateUserAsync(1, It.IsAny<UpdateUserRequest>()))
                .ReturnsAsync(existente);
            _repositoryMock.Setup(r => r.UpdateAsync(1, It.IsAny<User>()))
                .ReturnsAsync(Result.Failure<User, DomainError>(
                    new DomainError.DatabaseError("No se pudo actualizar")));

            // Act
            var r = _service.UpdateAsync(1, CrearRequestUpdateValido()).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.DatabaseError>();
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
            _notificationMock.Verify(n => n.NotificarActualizado(1), Times.Never);
        }

        [Test]
        public void Delete_UsuarioInexistente_RetornaNotFound()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetByIdAsync(8))
                .ReturnsAsync(Result.Failure<User, DomainError>(new DomainError.NotFound("User", 8)));
            _apiMock.Setup(a => a.GetUsersByIdAsync(8)).ReturnsAsync((User?)null);

            // Act
            var r = _service.DeleteAsync(8).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.NotFound>();
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            _apiMock.Verify(a => a.DeleteUserAsync(It.IsAny<int>()), Times.Never);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
            _notificationMock.Verify(n => n.NotificarEliminado(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Delete_ApiLanzaExcepcion_RetornaDatabaseError()
        {
            // Arrange
            var existente = CrearUsuarioValido(1);
            _repositoryMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(Result.Success<User, DomainError>(existente));
            _apiMock.Setup(a => a.DeleteUserAsync(1)).ThrowsAsync(new HttpRequestException("API caída"));

            // Act
            var r = _service.DeleteAsync(1).GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.DatabaseError>();
            _notificationMock.Verify(n => n.NotificarEliminado(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Export_RepoLanzaExcepcion_RetornaDatabaseError()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException("BD caída"));

            // Act
            var r = _service.ExportAsync().GetAwaiter().GetResult();

            // Assert
            r.IsFailure.Should().BeTrue();
            r.Error.Should().BeOfType<DomainError.DatabaseError>();
        }
    }
}