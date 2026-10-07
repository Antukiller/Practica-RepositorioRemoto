using System.Text.Json;
using Practicas_RepositorioRemoto.Dto.ModelDto;
using Practicas_RepositorioRemoto.Models;

namespace Practicas_RepositorioRemoto.Mapper;

public static class AddressMapper {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    
    private static Address DefaultCompany => new(
        Street: string.Empty,
        Suite: string.Empty,
        City: string.Empty,
        ZipCode: string.Empty,
        Geo: new Geo(Lat: string.Empty, Lng: string.Empty)
    );
    
    public static string ToJson(this Address? address) {
        if (address is null) return string.Empty;
        
        return JsonSerializer.Serialize(address, JsonOptions);
    }
    public static Address ToAddress(this string? json) {
        if (string.IsNullOrWhiteSpace(json)) 
            return DefaultCompany;
        try {
            var result = JsonSerializer.Deserialize<Address>(json, JsonOptions);
            return result ?? DefaultCompany;
        } catch (JsonException) {
            return DefaultCompany;
        }
    }
    public static Address ToModel(this AddressDto dto) {
        return new Address(
            Street: dto.Street,
            Suite: dto.Suite,
            City: dto.City,
            ZipCode: dto.ZipCode,
            Geo: new Geo(
                Lat: dto.Geo.Lat,
                Lng: dto.Geo.Lng
            )
        );
    }
}