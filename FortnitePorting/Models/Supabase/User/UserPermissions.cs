using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.OnlineServices.Packet;

namespace FortnitePorting.Models.Supabase.User;

public partial class UserPermissions : ObservableObject
{
    [ObservableProperty] private ESupabaseRole _role;
    [ObservableProperty] private bool _muted;
}