using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Mapper;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Test.Mappers;

/// <summary>
/// Tests de AddressMapper: serialización a JSON, deserialización
/// y conversión desde AddressDto.
/// </summary>
[TestFixture]
public class AddressMapperTests {

    private const string JsonValido = """
        {
            "street": "Kulas Light",
            "suite": "Apt. 556",
            "city": "Gwenborough",
            "zipCode": "92998-3874",
            "geo": {
                "lat": "-37.3159",
                "lng": "81.1496"
            }
        }
        """;

    private static AddressDto CrearDto(string json) {
        return JsonSerializer.Deserialize<AddressDto>(
            json,
            new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            }
        )!;
    }

    [TestFixture]
    public sealed class CasosValidos {

        private Address _address = null!;

        [SetUp]
        public void Setup() {
            _address = new Address(
                Street: "Kulas Light",
                Suite: "Apt. 556",
                City: "Gwenborough",
                ZipCode: "92998-3874",
                Geo: new Geo(
                    Lat: "-37.3159",
                    Lng: "81.1496"
                )
            );
        }

        [Test]
        public void ToJson_AddressValido_ConvierteCorrectamente() {
            //Act
            var resultado = _address.ToJson();

            //Assert
            resultado.Should().NotBeNullOrEmpty();

            using var documento = JsonDocument.Parse(resultado);
            var raiz = documento.RootElement;

            raiz.GetProperty("street").GetString().Should().Be(_address.Street);
            raiz.GetProperty("suite").GetString().Should().Be(_address.Suite);
            raiz.GetProperty("city").GetString().Should().Be(_address.City);
            raiz.GetProperty("zipCode").GetString().Should().Be(_address.ZipCode);

            var geo = raiz.GetProperty("geo");

            geo.GetProperty("lat").GetString().Should().Be(_address.Geo.Lat);
            geo.GetProperty("lng").GetString().Should().Be(_address.Geo.Lng);
        }

        [Test]
        public void ToJson_AddressValido_UtilizaCamelCase() {
            //Act
            var resultado = _address.ToJson();

            //Assert
            using var documento = JsonDocument.Parse(resultado);
            var raiz = documento.RootElement;

            raiz.TryGetProperty("street", out _).Should().BeTrue();
            raiz.TryGetProperty("zipCode", out _).Should().BeTrue();
            raiz.TryGetProperty("geo", out _).Should().BeTrue();

            raiz.TryGetProperty("Street", out _).Should().BeFalse();
            raiz.TryGetProperty("ZipCode", out _).Should().BeFalse();
            raiz.TryGetProperty("Geo", out _).Should().BeFalse();
        }

        [Test]
        public void ToJson_AddressValido_NoIncluyeSaltosDeLinea() {
            //Act
            var resultado = _address.ToJson();

            //Assert
            resultado.Should().NotContain("\n");
            resultado.Should().NotContain("\r");
        }

        [Test]
        public void ToAddress_JsonValido_ConvierteCorrectamente() {
            //Arrange
            var json = JsonValido;

            //Act
            var resultado = json.ToAddress();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_address);
        }

        [Test]
        public void ToJsonYToAddress_AddressValido_MantieneDatos() {
            //Act
            var json = _address.ToJson();
            var resultado = json.ToAddress();

            //Assert
            resultado.Should().BeEquivalentTo(_address);
            resultado.Should().NotBeSameAs(_address);
        }

        [Test]
        public void ToJsonYToAddress_ConCaracteresEspeciales_MantieneDatos() {
            //Arrange
            var address = new Address(
                Street: "Calle \"Alcalá\"\nNúmero 10",
                Suite: "Piso 2\\B",
                City: "Málaga",
                ZipCode: "29001",
                Geo: new Geo(
                    Lat: "36.7213",
                    Lng: "-4.4214"
                )
            );

            //Act
            var json = address.ToJson();
            var resultado = json.ToAddress();

            //Assert
            resultado.Should().BeEquivalentTo(address);
        }

        [Test]
        public void ToModel_DtoValido_ConvierteTodosLosCampos() {
            //Arrange
            var dto = CrearDto(JsonValido);

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_address);
        }

        [Test]
        public void ToModel_DtoValido_CreaNuevaInstanciaDeGeo() {
            //Arrange
            var dto = CrearDto(JsonValido);

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Geo.Should().NotBeNull();
            resultado.Geo.Should().NotBeSameAs(dto.Geo);
            resultado.Geo.Lat.Should().Be(dto.Geo.Lat);
            resultado.Geo.Lng.Should().Be(dto.Geo.Lng);
        }

        [Test]
        public void ToModel_DtoConCadenasVacias_ConservaCadenasVacias() {
            //Arrange
            var dto = CrearDto("""
                {
                    "street": "",
                    "suite": "",
                    "city": "",
                    "zipCode": "",
                    "geo": {
                        "lat": "",
                        "lng": ""
                    }
                }
                """);

            var esperado = new Address(
                Street: string.Empty,
                Suite: string.Empty,
                City: string.Empty,
                ZipCode: string.Empty,
                Geo: new Geo(
                    Lat: string.Empty,
                    Lng: string.Empty
                )
            );

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Should().BeEquivalentTo(esperado);
        }
    }

    [TestFixture]
    public sealed class CasosInvalidos {

        private Address _addressPorDefecto = null!;

        [SetUp]
        public void Setup() {
            _addressPorDefecto = new Address(
                Street: string.Empty,
                Suite: string.Empty,
                City: string.Empty,
                ZipCode: string.Empty,
                Geo: new Geo(
                    Lat: string.Empty,
                    Lng: string.Empty
                )
            );
        }

        [Test]
        public void ToJson_AddressNulo_RetornaCadenaVacia() {
            //Arrange
            Address? address = null;

            //Act
            var resultado = address.ToJson();

            //Assert
            resultado.Should().BeEmpty();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        [TestCase("\r\n")]
        public void ToAddress_JsonNuloVacioOBlanco_RetornaAddressPorDefecto(string? json) {
            //Act
            var resultado = json.ToAddress();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_addressPorDefecto);
        }

        [Test]
        public void ToAddress_JsonConLiteralNull_RetornaAddressPorDefecto() {
            //Arrange
            const string json = "null";

            //Act
            var resultado = json.ToAddress();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_addressPorDefecto);
        }

        [TestCase("{ esto no es un json valido }")]
        [TestCase("{")]
        [TestCase("{\"street\":")]
        [TestCase("texto sin formato JSON")]
        public void ToAddress_JsonMalFormado_RetornaAddressPorDefecto(string json) {
            //Act
            var resultado = json.ToAddress();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_addressPorDefecto);
        }

        [TestCase("[]")]
        [TestCase("123")]
        [TestCase("true")]
        [TestCase("\"texto\"")]
        [TestCase("{\"street\":123}")]
        [TestCase("{\"geo\":\"coordenadas incorrectas\"}")]
        public void ToAddress_JsonConTiposIncompatibles_RetornaAddressPorDefecto(string json) {
            //Act
            var resultado = json.ToAddress();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(_addressPorDefecto);
        }

        [Test]
        public void ToAddress_EntradasNulas_CreaInstanciasPorDefectoIndependientes() {
            //Arrange
            string? json = null;

            //Act
            var primero = json.ToAddress();
            var segundo = json.ToAddress();

            //Assert
            primero.Should().BeEquivalentTo(_addressPorDefecto);
            segundo.Should().BeEquivalentTo(_addressPorDefecto);

            primero.Should().NotBeSameAs(segundo);
            primero.Geo.Should().NotBeSameAs(segundo.Geo);
        }

        [Test]
        public void ToModel_DtoNulo_LanzaNullReferenceException() {
            //Arrange
            AddressDto dto = null!;

            //Act
            Action accion = () => {
                _ = dto.ToModel();
            };

            //Assert
            accion.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void ToModel_DtoConGeoNulo_LanzaNullReferenceException() {
            //Arrange
            var dto = CrearDto("""
                {
                    "street": "Kulas Light",
                    "suite": "Apt. 556",
                    "city": "Gwenborough",
                    "zipCode": "92998-3874",
                    "geo": null
                }
                """);

            //Act
            Action accion = () => {
                _ = dto.ToModel();
            };

            //Assert
            accion.Should().Throw<NullReferenceException>();
        }
    }
}