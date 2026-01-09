using Avalonia.Media;
using FortnitePorting.Models.Supabase.Tables;

namespace FortnitePorting.Models.API.Responses;

public class UserInfoResponse
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public ESupabaseRole Role { get; set; }
    
    public SolidColorBrush UserBrush => new(Role switch
    {
        ESupabaseRole.System => Color.Parse("#B040FF"),
        ESupabaseRole.Owner => Color.Parse("#83c4db"),
        ESupabaseRole.Support => Color.Parse("#635fd4"),
        ESupabaseRole.Staff => Color.Parse("#9856a2"),
        ESupabaseRole.Verified => Color.Parse("#00ff97"),
        ESupabaseRole.User => Colors.White,
        ESupabaseRole.Muted => Color.Parse("#d23940"),
        _ => Colors.White
    });
}