using Practicas_RepositorioRemoto.Dto;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Mapper;

public static class UsersMapper {
    public static User ToModel(this CreateUserRequest dto) {
        return new User {
            Name = dto.Name,
            UserName = dto.UserName,
            Email = dto.Email,
            Address = dto.Address.ToModel(),
            Phone = dto.Phone,
            Website = dto.Website,
            Company = dto.Company.ToModel(),
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            DeleteAt = default,
            IsDeleted = false

        };
    }
    public static User ToModel(this UpdateUserRequest dto) {
        return new User {
            Id = dto.Id,
            Name = dto.Name,
            UserName = dto.UserName,
            Email = dto.Email,
            Address = dto.Address.ToModel(),
            Phone = dto.Phone,
            Website = dto.Website,
            Company = dto.Company.ToModel(),
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
            DeleteAt = default,
            IsDeleted = false
        };
    }
}