using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Models.API.Responses;

public class IdentificationResponse
{
    [J("id")] public string Id;
    [J("avatar")] public string AvatarId;
    [J("username")] public string Username;
    [J("global_name")] public string GlobalName;
}