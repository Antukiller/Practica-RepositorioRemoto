namespace Practicas_RepositorioRemoto.Dto.ModelDto;

public record CompanyDto (
    string Street,
    string Suite,
    string City,
    string ZipCode, 
    GeoDto Geo
);