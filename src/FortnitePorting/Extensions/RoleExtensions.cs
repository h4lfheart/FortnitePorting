using Avalonia.Media;
using FortnitePorting.Models.Supabase.Tables;

namespace FortnitePorting.Extensions;

public static class RoleExtensions
{
    extension(ESupabaseRole role)
    {
        public SolidColorBrush Brush(bool isMuted = false)
        {
            if (isMuted) return new SolidColorBrush(Color.Parse("#d23940"));

            return new SolidColorBrush(role switch
            {
                ESupabaseRole.System => Color.Parse("#B040FF"),
                ESupabaseRole.Owner => Color.Parse("#83c4db"),
                ESupabaseRole.Support => Color.Parse("#635fd4"),
                ESupabaseRole.Staff => Color.Parse("#9856a2"),
                ESupabaseRole.Verified => Color.Parse("#00ff97"),
                ESupabaseRole.User => Colors.White,
                _ => Colors.White
            });
        }
    }

    extension(ESupabaseRole? role)
    {
        public SolidColorBrush Brush(bool isMuted = false) => (role ?? ESupabaseRole.User).Brush(isMuted);
    }
}
