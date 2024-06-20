using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Models.API.Responses;

public class OAuthResponse
{
    [J("token_type")] public string TokenType;
    [J("access_token")] public string AccessToken;
    [J("expires_in")] public int ExpiresIn;
    [J("refresh_token")] public string RefreshToken;
    [J("scope")] public string Scope;
}