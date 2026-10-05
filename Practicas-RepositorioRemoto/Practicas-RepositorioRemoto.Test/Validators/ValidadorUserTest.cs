using CSharpFunctionalExtensions; 
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Validators;

namespace Practicas_RepositorioRemoto.Test.Validators;

// ─── VALIDADORUSER TESTS ───────────────────────────────────────────

/// <summary>
/// Tests for ValidadorUser covering id, name, user name, email, phone, website,
/// audit dates and the delegation to the nested Address and Company validators.
/// </summary>
[TestFixture]
public class ValidadorUserTest
{
    /// <summary>Crea un usuario válido que sirve de base para los casos de prueba.</summary>
    private static User CrearUserValido() => new(
        Id: 1,
        Name: "Leanne Graham",
        UserName: "Bret",
        Email: "bret@srav.com",
        Address: new Address("Calle Mayor", "Apt. 123", "Madrid", "28001", new Geo("40.4168", "-3.7038")),
        Phone: "600 12 34 56",
        Website: "https://hildegard.org",
        Company: new Company("Acme Corp", "Slogan", "negocios hodie"),
        CreateAt: DateTime.Today.AddDays(-1),
        UpdateAt: DateTime.Today,
        DeleteAt: default,
        IsDeleted: false);

    private static Address AddressValida() =>
        new("Calle Mayor", "Apt. 123", "Madrid", "28001", new Geo("40.4168", "-3.7038"));

    private static Company CompanyValida() => new("Acme Corp", "Slogan", "negocios hodie");

    [TestFixture]
    public class CasosPositivos
    {
        private ValidadorUser _validador = null!;

        [SetUp]
        public void SetUp() => _validador = new ValidadorUser(new ValidadorAddress(), new ValidadorCompany());

        [Test]
        public void Validar_UserValido_DeberiaRetornarSuccess()
        {
            // Arrange
            var user = CrearUserValido();

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [TestCase("600123456")]
        [TestCase("+34 600 12 34 56")]
        [TestCase("600-12-34-56")]
        [TestCase("912 34 56 78")]
        public void Validar_TelefonoEspanol_DeberiaRetornarSuccess(string telefono)
        {
            // Arrange
            var user = CrearUserValido() with { Phone = telefono };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [TestCase("bret@srav.com")]
        [TestCase("jorge.o.smith@correo.es")]
        [TestCase("user+tag@sub.dominio.com")]
        public void Validar_EmailValido_DeberiaRetornarSuccess(string email)
        {
            // Arrange
            var user = CrearUserValido() with { Email = email };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [TestCase("hildegard.org")] // Sin esquema, como lo devuelve JSONPlaceholder
        [TestCase("https://hildegard.org")]
        [TestCase("http://www.ejemplo.com/pagina")]
        public void Validar_WebsiteValido_DeberiaRetornarSuccess(string website)
        {
            // Arrange
            var user = CrearUserValido() with { Website = website };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [TestCase("Bret")]
        [TestCase("jorge.o")]
        [TestCase("user_1")]
        public void Validar_UserNameValido_DeberiaRetornarSuccess(string userName)
        {
            // Arrange
            var user = CrearUserValido() with { UserName = userName };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public void Validar_UsuarioEliminado_DeberiaRetornarSuccess()
        {
            // Arrange
            var user = CrearUserValido() with { IsDeleted = true, DeleteAt = DateTime.Today };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        private ValidadorUser _validador = null!;

        [SetUp]
        public void SetUp() => _validador = new ValidadorUser(new ValidadorAddress(), new ValidadorCompany());

        [Test]
        public void Validar_IdNegativo_DeberiaRetornarFailure()
        {
            // Arrange
            var user = CrearUserValido() with { Id = -1 };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("identificador"));
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase(null)]
        public void Validar_Name_DeberiaRetornarFailure(string? nombre)
        {
            // Arrange
            var user = CrearUserValido() with { Name = nombre! };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("nombre es obligatorio"));
        }

        [TestCase("jorge o")] // Espacio no permitido
        [TestCase("user@correo")] // @ no permitido
        [TestCase("ñandú")] // Acentos no permitidos
        public void Validar_UserNameFormatoInvalido_DeberiaRetornarFailure(string userName)
        {
            // Arrange
            var user = CrearUserValido() with { UserName = userName };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("alfanuméricos"));
        }

        [TestCase("bret")]
        [TestCase("bret@")]
        [TestCase("@srav.com")]
        [TestCase("bret@srav")]
        [TestCase("bret @srav.com")]
        [TestCase("")]
        public void Validar_EmailInvalido_DeberiaRetornarFailure(string email)
        {
            // Arrange
            var user = CrearUserValido() with { Email = email };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("email"));
        }

        [TestCase("1-770-736-7631")] // Formato US devuelto por JSONPlaceholder
        [TestCase("123456789")] // No empieza por 6, 7, 8 ni 9
        [TestCase("60012345")] // Solo 8 dígitos
        [TestCase("")]
        [TestCase(" ")]
        public void Validar_TelefonoNoEspanol_DeberiaRetornarFailure(string telefono)
        {
            // Arrange
            var user = CrearUserValido() with { Phone = telefono };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("formato español"));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("no es una url")]
        public void Validar_WebsiteInvalido_DeberiaRetornarFailure(string website)
        {
            // Arrange
            var user = CrearUserValido() with { Website = website };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("sitio web"));
        }

        [Test]
        public void Validar_FechaCreacionFutura_DeberiaRetornarFailure()
        {
            // Arrange
            var user = CrearUserValido() with { CreateAt = DateTime.Today.AddDays(1) };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("La fecha de creación no puede ser futura.");
        }

        [Test]
        public void Validar_FechaActualizacionAnteriorACreacion_DeberiaRetornarFailure()
        {
            // Arrange
            var user = CrearUserValido() with
            {
                CreateAt = DateTime.Today.AddDays(-5),
                UpdateAt = DateTime.Today.AddDays(-10)
            };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("La fecha de actualización no puede ser anterior a la de creación.");
        }

        [Test]
        public void Validar_NoEliminadoConDeleteAt_DeberiaRetornarFailure()
        {
            // Arrange
            var user = CrearUserValido() with { IsDeleted = false, DeleteAt = DateTime.Today };

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("no debe tener fecha de eliminación"));
        }

        [Test]
        public void Validar_UserNulo_DeberiaRetornarFailure()
        {
            // Arrange
            User user = null!;

            // Act
            var result = _validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("El usuario es obligatorio.");
        }
    }

    [TestFixture]
    public class CasosDeDelegacion
    {
        [Test]
        public void Validar_DelegarEnAddressInvalida_DeberiaRetornarFailure()
        {
            // Arrange
            var mockAddress = new Mock<IValidador<Address>>();
            mockAddress
                .Setup(v => v.Validar(It.IsAny<Address>()))
                .Returns(Result.Failure<Address, DomainError>(
                    new Validation(["Error simulado de dirección"])));

            var validador = new ValidadorUser(mockAddress.Object, new ValidadorCompany());

            var user = CrearUserValido() with { Address = AddressValida() };

            // Act
            var result = validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("Error simulado de dirección");
            mockAddress.Verify(v => v.Validar(It.IsAny<Address>()), Times.Once);
        }

        [Test]
        public void Validar_DelegarEnCompanyInvalida_DeberiaRetornarFailure()
        {
            // Arrange
            var mockCompany = new Mock<IValidador<Company>>();
            mockCompany
                .Setup(v => v.Validar(It.IsAny<Company>()))
                .Returns(Result.Failure<Company, DomainError>(
                    new Validation(["Error simulado de compañía"])));

            var validador = new ValidadorUser(new ValidadorAddress(), mockCompany.Object);

            var user = CrearUserValido() with { Company = CompanyValida() };

            // Act
            var result = validador.Validar(user);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("Error simulado de compañía");
            mockCompany.Verify(v => v.Validar(It.IsAny<Company>()), Times.Once);
        }

        [Test]
        public void Validar_DelegacionValida_NoDebeReportarErrorDeAnidados()
        {
            // Arrange
            var mockAddress = new Mock<IValidador<Address>>();
            mockAddress
                .Setup(v => v.Validar(It.IsAny<Address>()))
                .Returns(Result.Success<Address, DomainError>(AddressValida()));

            var mockCompany = new Mock<IValidador<Company>>();
            mockCompany
                .Setup(v => v.Validar(It.IsAny<Company>()))
                .Returns(Result.Success<Company, DomainError>(CompanyValida()));

            var validador = new ValidadorUser(mockAddress.Object, mockCompany.Object);

            // Act
            var result = validador.Validar(CrearUserValido());

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockAddress.Verify(v => v.Validar(It.IsAny<Address>()), Times.Once);
            mockCompany.Verify(v => v.Validar(It.IsAny<Company>()), Times.Once);
        }
    }
}
