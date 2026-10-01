using Practicas_RepositorioRemoto.Dto.ModelDto;

namespace Practicas_RepositorioRemoto.Dto;

public record UpdateUserRepuest (
    int Id,
    string Name,
    string UserName,
    string Email,
    AddressDto Address,
    string Phone,
    string Website,
    CompanyDto Company);