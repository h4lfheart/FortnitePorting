using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class AuthResponse
{
    [JsonProperty("supabase_url")] public string SupabaseURL;
    [JsonProperty("supabase_anon_key")] public string SupabaseAnonKey;
}