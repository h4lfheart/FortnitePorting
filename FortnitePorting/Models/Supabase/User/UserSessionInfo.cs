namespace FortnitePorting.Models.Supabase.User;

public class UserSessionInfo(string accessToken, string refreshToken)
{
    public string AccessToken { get; set; } = accessToken;
    public string RefreshToken { get; set; } = refreshToken;
}