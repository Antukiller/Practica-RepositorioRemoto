using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Mapper;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Test.Mappers;

/// <summary>
/// Tests de CompanyMapper: serialización a JSON, deserialización
/// y conversión desde CompanyDto.
/// </summary>
[TestFixture]
public class CompanyMapperTests {

    private const string JsonValido = """
        {
            "name": "Romaguera-Crona",
            "catchPhrase": "Multi-layered client-server neural-net",
            "bs": "harness real-time e-markets"
        }
        """;

    private static CompanyDto CrearDto(string json) {
        return JsonSerializer.Deserialize<CompanyDto>(
            json,
            new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            }
        )!;
    }

    [TestFixture]
    public sealed class CasosValidos {

        private Company _company = null!;

        [SetUp]
        public void Setup() {
            _company = new Company(
                Name: "Romaguera-Crona",
                CatchPhrase: "Multi-layered client-server neural-net",
                Bs: "harness real-time e-markets"
            );
        }

        [Test]
        public void ToJson_CompanyValida_ConvierteCorrectamente() {
            //Act
            var resultado = _company.ToJson();

            //Assert
            resultado.Should().NotBeNullOrEmpty();

            using var documento = JsonDocument.Parse(resultado);
            var raiz = documento.RootElement;

            raiz.GetProperty("name").GetString().Should().Be(_company.Name);
            raiz.GetProperty("catchPhrase").GetString().Should().Be(_company.CatchPhrase);
            raiz.GetProperty("bs").GetString().Should().Be(_company.Bs);
        }

        [Test]
        public void ToJson_CompanyValida_UtilizaCamelCase() {
            //Act
            var resultado = _company.ToJson();

            //Assert
            using var documento = JsonDocument.Parse(resultado);
            var raiz = documento.RootElement;

            raiz.TryGetProperty("name", out _).Should().BeTrue();
            raiz.TryGetProperty("catchPhrase", out _).Should().BeTrue();
            raiz.TryGetProperty("bs", out _).Should().BeTrue();

            raiz.TryGetProperty("Name", out _).Should().BeFalse();
            raiz.TryGetProperty("CatchPhrase", out _).Should().BeFalse();
            raiz.TryGetProperty("Bs", out _).Should().BeFalse();
        }

        [Test]
        public void ToJson_CompanyValida_NoIncluyeSaltosDeLinea() {
            //Act
            var resultado = _company.ToJson();

            //Assert
            resultado.Should().NotContain("\n");
            resultado.Should().NotContain("\r");
        }

        [Test]
        public void ToCompany_JsonValido_ConvierteCorrectamente() {
            //Arrange
            var json = JsonValido;

            //Act
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Name.Should().Be(_company.Name);
            resultado.CatchPhrase.Should().Be(_company.CatchPhrase);
            resultado.Bs.Should().Be(_company.Bs);
        }

        [Test]
        public void ToJsonYToCompany_CompanyValida_MantieneDatos() {
            //Act
            var json = _company.ToJson();
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().BeEquivalentTo(_company);
            resultado.Should().NotBeSameAs(_company);
        }

        [Test]
        public void ToJsonYToCompany_ConCaracteresEspeciales_MantieneDatos() {
            //Arrange
            var company = new Company(
                Name: "Compañía \"Málaga\"",
                CatchPhrase: "Innovación\nTecnología",
                Bs: "Servicios\\Consultoría"
            );

            //Act
            var json = company.ToJson();
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().BeEquivalentTo(company);
        }

        [Test]
        public void ToJsonYToCompany_ConCadenasVacias_MantieneDatos() {
            //Arrange
            var company = new Company(
                Name: string.Empty,
                CatchPhrase: string.Empty,
                Bs: string.Empty
            );

            //Act
            var json = company.ToJson();
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().BeEquivalentTo(company);
        }

        [Test]
        public void ToModel_DtoValido_ConvierteTodosLosCampos() {
            //Arrange
            var dto = CrearDto(JsonValido);

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Name.Should().Be(_company.Name);
            resultado.CatchPhrase.Should().Be(_company.CatchPhrase);
            resultado.Bs.Should().Be(_company.Bs);
        }

        [Test]
        public void ToModel_DtoConCadenasVacias_ConservaCadenasVacias() {
            //Arrange
            var dto = CrearDto("""
                {
                    "name": "",
                    "catchPhrase": "",
                    "bs": ""
                }
                """);

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Name.Should().BeEmpty();
            resultado.CatchPhrase.Should().BeEmpty();
            resultado.Bs.Should().BeEmpty();
        }

        [Test]
        public void ToModel_MismoDto_CreaInstanciasIndependientes() {
            //Arrange
            var dto = CrearDto(JsonValido);

            //Act
            var primero = dto.ToModel();
            var segundo = dto.ToModel();

            //Assert
            primero.Should().BeEquivalentTo(_company);
            segundo.Should().BeEquivalentTo(_company);
            primero.Should().NotBeSameAs(segundo);
        }
    }

    [TestFixture]
    public sealed class CasosInvalidos {

        private Company _companyPorDefecto = null!;

        [SetUp]
        public void Setup() {
            _companyPorDefecto = new Company(
                Name: string.Empty,
                CatchPhrase: string.Empty,
                Bs: string.Empty
            );
        }

        [Test]
        public void ToJson_CompanyNula_RetornaCadenaVacia() {
            //Arrange
            Company? company = null;

            //Act
            var resultado = company.ToJson();

            //Assert
            resultado.Should().BeEmpty();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        [TestCase("\r\n")]
        public void ToCompany_JsonNuloVacioOBlanco_RetornaCompanyPorDefecto(string? json) {
            //Act
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_companyPorDefecto);
        }

        [Test]
        public void ToCompany_JsonConLiteralNull_RetornaCompanyPorDefecto() {
            //Arrange
            const string json = "null";

            //Act
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_companyPorDefecto);
        }

        [TestCase("{ esto no es un json valido }")]
        [TestCase("{")]
        [TestCase("{\"name\":")]
        [TestCase("texto sin formato JSON")]
        public void ToCompany_JsonMalFormado_RetornaCompanyPorDefecto(string json) {
            //Act
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_companyPorDefecto);
        }

        [TestCase("[]")]
        [TestCase("123")]
        [TestCase("true")]
        [TestCase("\"texto\"")]
        [TestCase("{\"name\":123}")]
        [TestCase("{\"catchPhrase\":true}")]
        [TestCase("{\"bs\":[]}")]
        public void ToCompany_JsonConTiposIncompatibles_RetornaCompanyPorDefecto(string json) {
            //Act
            var resultado = json.ToCompany();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_companyPorDefecto);
        }

        [Test]
        public void ToCompany_EntradasNulas_CreaInstanciasPorDefectoIndependientes() {
            //Arrange
            string? json = null;

            //Act
            var primero = json.ToCompany();
            var segundo = json.ToCompany();

            //Assert
            primero.Should().BeEquivalentTo(_companyPorDefecto);
            segundo.Should().BeEquivalentTo(_companyPorDefecto);
            primero.Should().NotBeSameAs(segundo);
        }

        [Test]
        public void ToModel_DtoNulo_LanzaNullReferenceException() {
            //Arrange
            CompanyDto dto = null!;

            //Act
            Action accion = () => {
                _ = dto.ToModel();
            };

            //Assert
            accion.Should().Throw<NullReferenceException>();
        }
    }
}