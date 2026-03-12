using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Supabase.User;

public class UserSessionInfo(string accessToken, string refreshToken)
{
    public string AccessToken { get; set; } = accessToken;
    public string RefreshToken { get; set; } = refreshToken;

    private static readonly IDataProtector Protector = DataProtectionProvider
        .Create("FortnitePorting")
        .CreateProtector("SessionTokens");

    public string ToEncryptedString() =>
        Protector.Protect(JsonConvert.SerializeObject(this));

    public static UserSessionInfo? FromEncryptedString(string encrypted)
    {
        try
        {
            var json = Protector.Unprotect(encrypted);
            return JsonConvert.DeserializeObject<UserSessionInfo>(json);
        }
        catch
        {
            return null;
        }
    }
}