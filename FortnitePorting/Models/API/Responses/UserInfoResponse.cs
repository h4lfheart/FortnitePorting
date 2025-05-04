using System;

namespace FortnitePorting.Models.API.Responses;

public class UserInfoResponse
{
    public string UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarURL { get; set; }
}