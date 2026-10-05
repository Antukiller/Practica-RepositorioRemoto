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
/// Tests de integración de UserRepositoryPostgre usando Testcontainers.
/// </summary>
[TestFixture]
public class UserRepositoryPostgreTests
{
    private PostgreSqlContainer _container = null!;
    private AppDbContextPostgre _context = null!;
    private UserRepositoryPostgre _repository = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("test_db")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContextPostgre>();
        optionsBuilder.UseNpgsql(_container.GetConnectionString());

        _context = new AppDbContextPostgre(optionsBuilder.Options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new UserRepositoryPostgre(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

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

    // ==========================================
    // CASOS POSITIVOS
    // ==========================================

    [Test]
    public async Task Create_UsuarioValido_DeberiaCrearConIdGenerado()
    {
        var usuario = CrearUsuarioValido();

        var resultado = await _repository.CreateAsync(usuario);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BeGreaterThan(0);
        resultado.Value.Name.Should().Be("Leanne Graham");
        resultado.Value.Email.Should().Be("bret@srav.com");
    }

    [Test]
    public async Task GetAll_ConDatos_DeberiaRetornarSoloActivos()
    {
        await _repository.CreateAsync(CrearUsuarioValido() with { Name = "Usuario A" });
        await _repository.CreateAsync(CrearUsuarioValido() with { Name = "Usuario B" });

        var resultados = await _repository.GetAllAsync();

        resultados.Should().HaveCount(2);
        resultados.Select(u => u.Name).Should().BeEquivalentTo(["Usuario A", "Usuario B"]);
    }

    [Test]
    public async Task GetById_Existente_DeberiaRetornarUsuario()
    {
        var creado = await _repository.CreateAsync(CrearUsuarioValido() with { Name = "Clementine Bauch" });

        var encontrado = await _repository.GetByIdAsync(creado.Value.Id);

        encontrado.IsSuccess.Should().BeTrue();
        encontrado.Value.Name.Should().Be("Clementine Bauch");
        encontrado.Value.UserName.Should().Be("Bret");
    }

    [Test]
    public async Task GetById_DeberiaPersistirAddressYCompany()
    {
        var creado = await _repository.CreateAsync(CrearUsuarioValido());

        var encontrado = await _repository.GetByIdAsync(creado.Value.Id);

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
        var creado = await _repository.CreateAsync(CrearUsuarioValido());

        var resultado = await _repository.UpdateAsync(creado.Value.Id,
            creado.Value with { Email = "nuevo@srav.com", Name = "Nombre Actualizado" });

        resultado.IsSuccess.Should().BeTrue();

        var releido = await _repository.GetByIdAsync(creado.Value.Id);
        releido.IsSuccess.Should().BeTrue();
        releido.Value.Email.Should().Be("nuevo@srav.com");
        releido.Value.Name.Should().Be("Nombre Actualizado");
    }

    [Test]
    public async Task Delete_Existente_DeberiaMarcarComoEliminadoYOcultarloEnGetById()
    {
        var creado = await _repository.CreateAsync(CrearUsuarioValido());

        var resultado = await _repository.DeleteAsync(creado.Value.Id);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.IsDeleted.Should().BeTrue();
        resultado.Value.DeleteAt.Should().NotBe(default);

        // El borrado es lógico: GetByIdAsync debe devolver NotFound porque ya no está activo
        var releidoRepositorio = await _repository.GetByIdAsync(creado.Value.Id);
        releidoRepositorio.IsFailure.Should().BeTrue();
        releidoRepositorio.Error.Should().BeOfType<DomainError.NotFound>();

        // Verificamos directamente en la BD que la fila sigue existiendo con IsDeleted = true
        var usuarioEnBd = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == creado.Value.Id);
        usuarioEnBd.Should().NotBeNull();
        usuarioEnBd!.IsDeleted.Should().BeTrue();
    }

    // ==========================================
    // CASOS NEGATIVOS
    // ==========================================

    [Test]
    public async Task GetById_Inexistente_DeberiaRetornarNotFound()
    {
        var resultado = await _repository.GetByIdAsync(9999);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<DomainError.NotFound>();
    }

    [Test]
    public async Task Update_Inexistente_DeberiaRetornarNotFound()
    {
        var resultado = await _repository.UpdateAsync(9999, CrearUsuarioValido(9999));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<DomainError.NotFound>();
    }

    [Test]
    public async Task Delete_Inexistente_DeberiaRetornarNotFound()
    {
        var resultado = await _repository.DeleteAsync(9999);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<DomainError.NotFound>();
    }

    [Test]
    public async Task Update_UsuarioYaEliminado_DeberiaRetornarNotFound()
    {
        var creado = await _repository.CreateAsync(
            CrearUsuarioValido() with { IsDeleted = true, DeleteAt = DateTime.UtcNow });

        var resultado = await _repository.UpdateAsync(creado.Value.Id, creado.Value);

        // Al estar borrado lógicamente, el repositorio lo trata como no existente para actualización
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<DomainError.NotFound>();
    }

    [Test]
    public async Task Create_NombreDemasiadoLargo_DeberiaRetornarDatabaseError()
    {
        var usuario = CrearUsuarioValido() with { Name = new string('a', 200) };

        var resultado = await _repository.CreateAsync(usuario);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<DomainError.DatabaseError>();
    }
}