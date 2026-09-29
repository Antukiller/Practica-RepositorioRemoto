namespace Practicas_RepositorioRemoto.Dto;

public record CreateUserRequest(
    string Name,
    string Username,
    string Email
    );