namespace Practicas_RepositorioRemoto.Dto;

public record UpdateUserRepuest (
    int Id,
    string Name,
    string Username,
    string Email
    );