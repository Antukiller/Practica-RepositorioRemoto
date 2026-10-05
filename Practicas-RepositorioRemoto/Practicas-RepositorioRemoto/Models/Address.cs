namespace Practicas_RepositorioRemoto.Models;


/// <summary>
/// Representa la direccion de un usuario dentro del sistema
/// </summary>
public record Address(
    string Street,
    string Suite,
    string City,
    string ZipCode,
    Geo Geo
) {
    public Address() : this("", "", "", "", new Geo()) { }
}