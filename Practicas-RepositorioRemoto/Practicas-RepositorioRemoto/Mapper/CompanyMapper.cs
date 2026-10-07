using System.Text.Json;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Mapper;

public static class CompanyMapper {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    
    private static Company DefaultCompany => new(
        Name: string.Empty,
        CatchPhrase: string.Empty,
        Bs: string.Empty
    );
    
    public static string ToJson(this Company? company) {
        if (company is null) return string.Empty;
        
        return JsonSerializer.Serialize(company, JsonOptions);
    }
    public static Company ToCompany(this string? json) {
        if (string.IsNullOrWhiteSpace(json)) 
            return DefaultCompany;
        try {
            var result = JsonSerializer.Deserialize<Company>(json, JsonOptions);
            return result ?? DefaultCompany;
        } catch (JsonException) {
            return DefaultCompany;
        }
    }
    public static Company ToModel(this CompanyDto dto) {
        return new Company(
            Name: dto.Name,
            CatchPhrase: dto.CatchPhrase,
            Bs: dto.Bs
        );
    }
    
}