using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class AuthResponse
{
    [JsonProperty("supabaseUrl")] public string SupabaseURL;
    [JsonProperty("supabaseAnonKey")] public string SupabaseAnonKey;
}