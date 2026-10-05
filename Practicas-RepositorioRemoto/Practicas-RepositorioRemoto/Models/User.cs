namespace Practicas_RepositorioRemoto.Models;

/// <summary>
///  Representa un usuario dentro del sistema.
/// </summary>
public record User(
    int Id,
    string Name,
    string UserName,
    string Email,
    Address Address,
    string Phone,
    string Website,
    Company Company,
    DateTime CreateAt,
    DateTime UpdateAt,
    DateTime DeleteAt,
    bool IsDeleted
) {
    public User() : this(0, "", "", "", new Address(), "", "", new Company(), default, default, default, false) { }
}