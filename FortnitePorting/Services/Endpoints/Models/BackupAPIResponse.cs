using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Services.Endpoints.Models;

public class BackupAPIResponse
{
    [J] public bool IsActive;
    [J] public AesResponse AES;
    [J] public MappingsResponse[] Mappings;

    public MappingsResponse[]? GetMappings()
    {
        return IsActive ? Mappings : default;
    }
    
    public AesResponse? GetKeys()
    {
        return IsActive ? AES : default;
    }
}