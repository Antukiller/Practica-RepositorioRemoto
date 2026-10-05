using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Entity;

/// <summary>
/// Contexto de Entity Framework Core para PostgreSQL.
/// </summary>
/// <param name="options">Opciones de configuración del contexto</param>
public class AppDbContextPostgre(DbContextOptions<AppDbContextPostgre> options) : DbContext(options)
{
    /// <summary>
    /// Opciones de serialización empleadas para los objetos de valor.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <summary>
    /// Traduce un <see cref="Address"/> a texto JSON para guardarlo en una columna.
    /// </summary>
    private static readonly ValueConverter<Address, string> AddressToJson = new(
        address => JsonSerializer.Serialize(address, JsonOptions),
        json => JsonSerializer.Deserialize<Address>(json, JsonOptions)!);

    /// <summary>
    /// Compara dos <see cref="Address"/> por su representación JSON porque EF no sabe
    /// comparar directamente este tipo, y clona mediante una copia deserializada.
    /// </summary>
    private static readonly ValueComparer<Address> AddressJsonComparer = new(
        (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
        a => JsonSerializer.Serialize(a, JsonOptions).GetHashCode(),
        a => JsonSerializer.Deserialize<Address>(JsonSerializer.Serialize(a, JsonOptions), JsonOptions)!);

    /// <summary>
    /// Traduce un <see cref="Company"/> a texto JSON para guardarlo en una columna.
    /// </summary>
    private static readonly ValueConverter<Company, string> CompanyToJson = new(
        company => JsonSerializer.Serialize(company, JsonOptions),
        json => JsonSerializer.Deserialize<Company>(json, JsonOptions)!);

    /// <summary>
    /// Compara dos <see cref="Company"/> por su representación JSON porque EF no sabe
    /// comparar directamente este tipo, y clona mediante una copia deserializada.
    /// </summary>
    private static readonly ValueComparer<Company> CompanyJsonComparer = new(
        (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
        a => JsonSerializer.Serialize(a, JsonOptions).GetHashCode(),
        a => JsonSerializer.Deserialize<Company>(JsonSerializer.Serialize(a, JsonOptions), JsonOptions)!);

    /// <summary>
    /// Conjunto de datos de usuarios.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Define el esquema de la tabla de usuarios.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo</param>
    /// <remarks>
    /// <see cref="Address"/> y <see cref="Company"/> se guardan como columnas jsonb en lugar de
    /// usarse <c>OwnsOne</c>. Al ser records posicionales, EF no encuentra un constructor
    /// enlazable para materializarlos como owned, por lo que se serializan como jsonb.
    /// <see cref="Models.Geo"/> queda incluido dentro del JSON de <see cref="Address"/>.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
                .HasMaxLength(150);

            e.Property(x => x.UserName)
                .HasMaxLength(200);

            e.Property(x => x.Email)
                .HasMaxLength(150);

            e.Property(x => x.Address)
                .HasConversion(AddressToJson)
                .HasColumnName("address")
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(AddressJsonComparer);

            e.Property(x => x.Phone)
                .IsRequired()
                .HasMaxLength(50);

            e.Property(x => x.Website)
                .HasMaxLength(300);

            e.Property(x => x.Company)
                .HasConversion(CompanyToJson)
                .HasColumnName("company")
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(CompanyJsonComparer);

            e.Property(x => x.CreateAt);
            e.Property(x => x.UpdateAt);
            e.Property(x => x.DeleteAt);
            e.Property(x => x.IsDeleted);
        });
    }
}