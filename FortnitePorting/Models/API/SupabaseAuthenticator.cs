using RestSharp;
using RestSharp.Authenticators;

namespace FortnitePorting.Models.API;

public class SupabaseAuthenticator() : AuthenticatorBase(string.Empty)
{
    protected override ValueTask<Parameter> GetAuthenticationParameter(string accessToken)
    {
        var token = SupaBase.Client?.Auth?.CurrentSession?.AccessToken ?? string.Empty;
        return ValueTask.FromResult<Parameter>(new HeaderParameter(KnownHeaders.Authorization, $"Bearer {token}"));
    }
}
