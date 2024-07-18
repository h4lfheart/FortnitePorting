using System;

namespace FortnitePorting.Models.API.Responses;

public class UserResponse
{
    public Guid Identifier { get; set; }
    public string GlobalName { get; set; }
    public string Username { get; set; }
    public string DiscordId { get; set; }
    public string AvatarId { get; set; }
}