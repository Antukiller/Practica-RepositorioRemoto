using Practicas_RepositorioRemoto.Errors;
using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Validators;

namespace Practicas_RepositorioRemoto.Test.Validators;

// ─── VALIDADORCOMPANY TESTS ───────────────────────────────────────────

/// <summary>
///     Tests for ValidadorCompany covering company name, catch phrase and bs validation rules.
/// </summary>
[TestFixture]
public class ValidadorCompanyTest {
    [SetUp]
    public void SetUp() {
        _validador = new ValidadorCompany();
    }

    private ValidadorCompany _validador = null!;

    /// <summary>Crea una compañía válida que sirve de base para los casos de prueba.</summary>
    private static Company CrearCompanyValida() {
        return new Company(
            "Acme Corp",
            "Slogan de la compañía",
            "negocios hodie");
    }

    [TestFixture]
    public class CasosPositivos {
        [SetUp]
        public void SetUp() {
            _validador = new ValidadorCompany();
        }

        private ValidadorCompany _validador = null!;

        [Test]
        public void Validar_CompanyValido_DeberiaRetornarSuccess() {
            // Arrange
            var compania = CrearCompanyValida();

            // Act
            var result = _validador.Validar(compania);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [TestCase("Acme Corp")]
        [TestCase("A")] // Sin mínimo de caracteres
        public void Validar_NameNoVacio_DeberiaRetornarSuccess(string nombre) {
            // Arrange
            var compania = CrearCompanyValida() with { Name = nombre };

            // Act
            var result = _validador.Validar(compania);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        [SetUp]
        public void SetUp() {
            _validador = new ValidadorCompany();
        }

        private ValidadorCompany _validador = null!;

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void Validar_Name_DeberiaRetornarFailure(string? nombre) {
            // Arrange
            var compania = CrearCompanyValida() with { Name = nombre! };

            // Act
            var result = _validador.Validar(compania);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<Validation>();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("nombre de la compañía"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Validar_CatchPhrase_DeberiaRetornarFailure(string catchPhrase) {
            // Arrange
            var compania = CrearCompanyValida() with { CatchPhrase = catchPhrase };

            // Act
            var result = _validador.Validar(compania);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("eslogan"));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void Validar_Bs_DeberiaRetornarFailure(string bs) {
            // Arrange
            var compania = CrearCompanyValida() with { Bs = bs };

            // Act
            var result = _validador.Validar(compania);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain(e => e.Contains("lema de negocio"));
        }

        [Test]
        public void Validar_CompanyNulo_DeberiaRetornarFailure() {
            // Arrange
            Company compania = null!;

            // Act
            var result = _validador.Validar(compania);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().Contain("La compañía es obligatoria.");
        }

        [Test]
        public void Validar_TodosLosCamposEnBlanco_DeberiaAcumularTresErrores() {
            // Arrange
            var compania = new Company("", " ", "");

            // Act
            var result = _validador.Validar(compania);

            // Assert
            result.IsFailure.Should().BeTrue();
            var validationError = (Validation)result.Error;
            validationError.Errors.Should().HaveCount(3);
        }
    }
}