using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Validators;

namespace Practicas_RepositorioRemoto.Test.Validators;

// ─── VALIDADORADDRESS TESTS ───────────────────────────────────────────

/// <summary>
///     Tests for ValidadorAddress covering street, suite, city, zip code and geo validation rules.
/// </summary>
[TestFixture]
public class ValidadorAddressTest {
    /// <summary>Crea una dirección válida que sirve de base para los casos de prueba.</summary>
    private static Address CrearAddressValida() {
        return new Address(
            "Calle Mayor",
            "Apt. 123",
            "Madrid",
            "28001",
            new Geo("40.4168", "-3.7038"));
    }

    [TestFixture]
    public class CasosPositivos {
        [SetUp]
        public void SetUp() {
            _validador = new ValidadorAddress();
        }

        private ValidadorAddress _validador = null!;

        [Test]
        public void Validar_AddressValido_DeberiaRetornarSuccess() {
            // Arrange
            var direccion = CrearAddressValida();

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [TestCase("Calle Mayor")]
        [TestCase("A")] // Sin mínimo de caracteres
        [TestCase("Calle con un nombre tan largo como se quiera porque no se validan longitudes")]
        public void Validar_StreetNoVacio_DeberiaRetornarSuccess(string calle) {
            // Arrange
            var direccion = CrearAddressValida() with { Street = calle };

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [TestCase("92998-3874")] // Formato USA devuelto por JSONPlaceholder
        [TestCase("28001")]
        public void Validar_ZipCodeValido_DeberiaRetornarSuccess(string zipCode) {
            // Arrange
            var direccion = CrearAddressValida() with { ZipCode = zipCode };

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        [SetUp]
        public void SetUp() {
            _validador = new ValidadorAddress();
        }

        private ValidadorAddress _validador = null!;

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Validar_Street_DeberiaRetornarFailure(string? calle) {
            // Arrange
            var direccion = CrearAddressValida() with { Street = calle! };

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<Validation>();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("La calle es obligatoria y no puede estar en blanco.");
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Validar_Suite_DeberiaRetornarFailure(string suite) {
            // Arrange
            var direccion = CrearAddressValida() with { Suite = suite };

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("El bloque/piso es obligatorio y no puede estar en blanco.");
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Validar_City_DeberiaRetornarFailure(string ciudad) {
            // Arrange
            var direccion = CrearAddressValida() with { City = ciudad };

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("ciudad") && e.Contains("en blanco"));
        }

        [TestCase("2800")]
        [TestCase("ABCDE")]
        [TestCase("28001-")]
        public void Validar_ZipCode_DeberiaRetornarFailure(string zipCode) {
            // Arrange
            var direccion = CrearAddressValida() with { ZipCode = zipCode };

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("código postal"));
        }

        [TestCase("abc")] // Coordenadas no numéricas
        [TestCase("40,4168")] // Separador decimal incorrecto
        [TestCase("")] // Vacía
        public void Validar_GeoInvalido_DeberiaRetornarFailure(string lat) {
            // Arrange
            var direccion = CrearAddressValida() with { Geo = new Geo(lat, "-3.7038") };

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("geolocalización"));
        }

        [Test]
        public void Validar_AddressNulo_DeberiaRetornarFailure() {
            // Arrange
            Address direccion = null!;

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("La dirección es obligatoria.");
        }

        [Test]
        public void Validar_MultiplesErrores_DeberiaAcumularTodos() {
            // Arrange
            var direccion = new Address("", " ", "", "ABCDE", new Geo("abc", "xyz"));

            // Act
            var result = _validador.Validar(direccion);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().HaveCount(5);
        }
    }
}