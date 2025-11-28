using System;
using Avalonia.Media.Imaging;
using FortnitePorting.Models.Supabase.Tables;

namespace FortnitePorting.Models.API.Responses;

public class UserInfoResponse
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public ESupabaseRole Role { get; set; }
}