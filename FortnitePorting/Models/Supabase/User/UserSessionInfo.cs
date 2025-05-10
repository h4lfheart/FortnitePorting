namespace FortnitePorting.Models.Supabase.User;

public class UserSessionInfo(string accessToken, string refreshToken, string tag)
{
    public string Tag { get; set; } = tag;
    public string AccessToken { get; set; } = accessToken;
    public string RefreshToken { get; set; } = refreshToken;
}