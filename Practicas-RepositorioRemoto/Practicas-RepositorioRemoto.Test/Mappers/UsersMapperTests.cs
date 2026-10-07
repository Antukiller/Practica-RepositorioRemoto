using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Mapper;

namespace Practicas_RepositorioRemoto.Test.Mappers;

/// <summary>
/// Tests de UsersMapper: conversión de CreateUserRequest y UpdateUserRequest
/// a la entidad User del dominio.
/// </summary>
[TestFixture]
public class UsersMapperTests {

    private const string JsonUsuario = """
        {
            "id": 1,
            "name": "Leanne Graham",
            "userName": "Bret",
            "email": "Sincere@april.biz",
            "address": {
                "street": "Kulas Light",
                "suite": "Apt. 556",
                "city": "Gwenborough",
                "zipCode": "92998-3874",
                "geo": {
                    "lat": "-37.3159",
                    "lng": "81.1496"
                }
            },
            "phone": "1-770-736-7631",
            "website": "hildegard.org",
            "company": {
                "name": "Romaguera-Crona",
                "catchPhrase": "Multi-layered client-server neural-net",
                "bs": "harness real-time e-markets"
            }
        }
        """;

    private static T CrearDto<T>(string json) {
        return JsonSerializer.Deserialize<T>(
            json,
            new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            }
        )!;
    }

    [TestFixture]
    public sealed class CasosValidos {

        private CreateUserRequest _createDto = null!;
        private UpdateUserRequest _updateDto = null!;

        [SetUp]
        public void Setup() {
            _createDto = CrearDto<CreateUserRequest>(JsonUsuario);
            _updateDto = CrearDto<UpdateUserRequest>(JsonUsuario);
        }

        [Test]
        public void ToModel_CreateUserRequestValido_CopiaDatosDelUsuario() {
            //Act
            var resultado = _createDto.ToModel();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Name.Should().Be("Leanne Graham");
            resultado.UserName.Should().Be("Bret");
            resultado.Email.Should().Be("Sincere@april.biz");
            resultado.Phone.Should().Be("1-770-736-7631");
            resultado.Website.Should().Be("hildegard.org");
        }

        [Test]
        public void ToModel_CreateUserRequestValido_AsignaIdCero() {
            //Act
            var resultado = _createDto.ToModel();

            //Assert
            resultado.Id.Should().Be(0);
        }

        [Test]
        public void ToModel_CreateUserRequestValido_MapeaAddressYGeo() {
            //Act
            var resultado = _createDto.ToModel();

            //Assert
            resultado.Address.Should().NotBeNull();
            resultado.Address.Should().NotBeSameAs(_createDto.Address);

            resultado.Address.Street.Should().Be("Kulas Light");
            resultado.Address.Suite.Should().Be("Apt. 556");
            resultado.Address.City.Should().Be("Gwenborough");
            resultado.Address.ZipCode.Should().Be("92998-3874");

            resultado.Address.Geo.Should().NotBeNull();
            resultado.Address.Geo.Should().NotBeSameAs(_createDto.Address.Geo);
            resultado.Address.Geo.Lat.Should().Be("-37.3159");
            resultado.Address.Geo.Lng.Should().Be("81.1496");
        }

        [Test]
        public void ToModel_CreateUserRequestValido_MapeaCompany() {
            //Act
            var resultado = _createDto.ToModel();

            //Assert
            resultado.Company.Should().NotBeNull();
            resultado.Company.Should().NotBeSameAs(_createDto.Company);

            resultado.Company.Name.Should().Be("Romaguera-Crona");
            resultado.Company.CatchPhrase.Should().Be("Multi-layered client-server neural-net");
            resultado.Company.Bs.Should().Be("harness real-time e-markets");
        }

        [Test]
        public void ToModel_CreateUserRequestValido_AsignaFechasActualesUtc() {
            //Arrange
            var antes = DateTime.UtcNow;

            //Act
            var resultado = _createDto.ToModel();
            var despues = DateTime.UtcNow;

            //Assert
            resultado.CreateAt.Should().BeOnOrAfter(antes);
            resultado.CreateAt.Should().BeOnOrBefore(despues);
            resultado.CreateAt.Kind.Should().Be(DateTimeKind.Utc);

            resultado.UpdateAt.Should().BeOnOrAfter(antes);
            resultado.UpdateAt.Should().BeOnOrBefore(despues);
            resultado.UpdateAt.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Test]
        public void ToModel_CreateUserRequestValido_InicializaEstadoSinBorrar() {
            //Act
            var resultado = _createDto.ToModel();

            //Assert
            resultado.IsDeleted.Should().BeFalse();
            resultado.DeleteAt.Should().Be(default);
        }

        [Test]
        public void ToModel_UpdateUserRequestValido_CopiaIdYDatosDelUsuario() {
            //Act
            var resultado = _updateDto.ToModel();

            //Assert
            resultado.Should().NotBeNull();
            resultado.Id.Should().Be(1);
            resultado.Name.Should().Be("Leanne Graham");
            resultado.UserName.Should().Be("Bret");
            resultado.Email.Should().Be("Sincere@april.biz");
            resultado.Phone.Should().Be("1-770-736-7631");
            resultado.Website.Should().Be("hildegard.org");
        }

        [Test]
        public void ToModel_UpdateUserRequestValido_MapeaAddressYGeo() {
            //Act
            var resultado = _updateDto.ToModel();

            //Assert
            resultado.Address.Should().NotBeNull();
            resultado.Address.Should().NotBeSameAs(_updateDto.Address);

            resultado.Address.Street.Should().Be("Kulas Light");
            resultado.Address.Suite.Should().Be("Apt. 556");
            resultado.Address.City.Should().Be("Gwenborough");
            resultado.Address.ZipCode.Should().Be("92998-3874");

            resultado.Address.Geo.Should().NotBeNull();
            resultado.Address.Geo.Should().NotBeSameAs(_updateDto.Address.Geo);
            resultado.Address.Geo.Lat.Should().Be("-37.3159");
            resultado.Address.Geo.Lng.Should().Be("81.1496");
        }

        [Test]
        public void ToModel_UpdateUserRequestValido_MapeaCompany() {
            //Act
            var resultado = _updateDto.ToModel();

            //Assert
            resultado.Company.Should().NotBeNull();
            resultado.Company.Should().NotBeSameAs(_updateDto.Company);

            resultado.Company.Name.Should().Be("Romaguera-Crona");
            resultado.Company.CatchPhrase.Should().Be("Multi-layered client-server neural-net");
            resultado.Company.Bs.Should().Be("harness real-time e-markets");
        }

        [Test]
        public void ToModel_UpdateUserRequestValido_AsignaFechaActualizacionUtc() {
            //Arrange
            var antes = DateTime.UtcNow;

            //Act
            var resultado = _updateDto.ToModel();
            var despues = DateTime.UtcNow;

            //Assert
            resultado.UpdateAt.Should().BeOnOrAfter(antes);
            resultado.UpdateAt.Should().BeOnOrBefore(despues);
            resultado.UpdateAt.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Test]
        public void ToModel_CreateUserRequest_CreaInstanciasIndependientes() {
            //Act
            var primero = _createDto.ToModel();
            var segundo = _createDto.ToModel();

            //Assert
            primero.Should().NotBeSameAs(segundo);
            primero.Address.Should().NotBeSameAs(segundo.Address);
            primero.Address.Geo.Should().NotBeSameAs(segundo.Address.Geo);
            primero.Company.Should().NotBeSameAs(segundo.Company);
        }

        [Test]
        public void ToModel_UpdateUserRequest_CreaInstanciasIndependientes() {
            //Act
            var primero = _updateDto.ToModel();
            var segundo = _updateDto.ToModel();

            //Assert
            primero.Should().NotBeSameAs(segundo);
            primero.Address.Should().NotBeSameAs(segundo.Address);
            primero.Address.Geo.Should().NotBeSameAs(segundo.Address.Geo);
            primero.Company.Should().NotBeSameAs(segundo.Company);
        }
    }

    [TestFixture]
    public sealed class CasosInvalidos {

        [Test]
        public void ToModel_CreateUserRequestNulo_LanzaNullReferenceException() {
            //Arrange
            CreateUserRequest dto = null!;

            //Act
            Action accion = () => {
                _ = dto.ToModel();
            };

            //Assert
            accion.Should().Throw<NullReferenceException>();
        }

        [Test]
        public void ToModel_UpdateUserRequestNulo_LanzaNullReferenceException() {
            //Arrange
            UpdateUserRequest dto = null!;

            //Act
            Action accion = () => {
                _ = dto.ToModel();
            };

            //Assert
            accion.Should().Throw<NullReferenceException>();
        }

        [TestCase("address")]
        [TestCase("company")]
        public void ToModel_CreateUserRequestConObjetoNulo_LanzaNullReferenceException(string propiedad) {
            //Arrange
            var datos = JsonSerializer.Deserialize<
                System.Collections.Generic.Dictionary<string, JsonElement>
            >(JsonUsuario)!;

            datos[propiedad] = JsonSerializer.SerializeToElement<object?>(null);

            var dto = CrearDto<CreateUserRequest>(JsonSerializer.Serialize(datos));

            //Act
            Action accion = () => {
                _ = dto.ToModel();
            };

            //Assert
            accion.Should().Throw<NullReferenceException>();
        }

        [TestCase("address")]
        [TestCase("company")]
        public void ToModel_UpdateUserRequestConObjetoNulo_LanzaNullReferenceException(string propiedad) {
            //Arrange
            var datos = JsonSerializer.Deserialize<
                System.Collections.Generic.Dictionary<string, JsonElement>
            >(JsonUsuario)!;

            datos[propiedad] = JsonSerializer.SerializeToElement<object?>(null);

            var dto = CrearDto<UpdateUserRequest>(JsonSerializer.Serialize(datos));

            //Act
            Action accion = () => {
                _ = dto.ToModel();
            };

            //Assert
            accion.Should().Throw<NullReferenceException>();
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ToModel_UpdateUserRequestConIdNoPositivo_ConservaId(int id) {
            //Arrange
            var json = JsonUsuario.Replace("\"id\": 1", $"\"id\": {id}");
            var dto = CrearDto<UpdateUserRequest>(json);

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Id.Should().Be(id);
        }

        [TestCase("")]
        [TestCase("correo-invalido")]
        public void ToModel_CreateUserRequestConEmailInvalido_ConservaEmail(string email) {
            //Arrange
            var json = JsonUsuario.Replace("Sincere@april.biz", email);
            var dto = CrearDto<CreateUserRequest>(json);

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Email.Should().Be(email);
        }

        [TestCase("")]
        [TestCase("correo-invalido")]
        public void ToModel_UpdateUserRequestConEmailInvalido_ConservaEmail(string email) {
            //Arrange
            var json = JsonUsuario.Replace("Sincere@april.biz", email);
            var dto = CrearDto<UpdateUserRequest>(json);

            //Act
            var resultado = dto.ToModel();

            //Assert
            resultado.Email.Should().Be(email);
        }
    }
}