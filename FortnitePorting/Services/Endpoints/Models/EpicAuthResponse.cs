using System;
using Newtonsoft.Json;

namespace FortnitePorting.Services.Endpoints.Models;

public class EpicAuthResponse
{
    [JsonProperty("access_token")]
    public string AccessToken;
    [JsonProperty("expires_at")]
    public DateTime ExpiresAt;
}