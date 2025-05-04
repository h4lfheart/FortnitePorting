namespace FortnitePorting.Models.Supabase;

public class UserSessionInfo(string accessToken, string refreshToken)
{
    public string AccessToken = accessToken;
    public string RefreshToken = refreshToken;
}