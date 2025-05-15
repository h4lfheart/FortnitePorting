using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Models.Supabase.Tables;


namespace FortnitePorting.Models.Supabase.User;

public partial class UserPermissions : ObservableObject
{
    [ObservableProperty] private ESupabaseRole _role = ESupabaseRole.User;
    [ObservableProperty] private bool _canExportUEFN = false;
}