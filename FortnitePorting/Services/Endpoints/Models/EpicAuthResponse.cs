using System;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Services.Endpoints.Models;

public class EpicAuthResponse
{
    [J("access_token")] public string AccessToken;
    [J("expires_at")] public DateTime ExpiresAt;
}