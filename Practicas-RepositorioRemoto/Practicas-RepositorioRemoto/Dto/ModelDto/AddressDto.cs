namespace Practicas_RepositorioRemoto.Dto.ModelDto;

public record AddressDto (
    string Street,
    string Suite,
    string City,
    string ZipCode,
    GeoDto Geo
);