using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Supabase.Tables;
using Newtonsoft.Json;
using Supabase.Realtime.Models;

namespace FortnitePorting.Models.Chat;

public partial class ChatUserV2 : ObservableObject
{
    [ObservableProperty] private string _userId;
    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _avatarUrl;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private ESupabaseRole _role;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OnlineVersion))] private string _version;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OnlineVersion))] private string _tag;

    public string OnlineVersion => !string.IsNullOrWhiteSpace(Tag) ? $"{Tag} {Version}" : Version;

    public bool CanChangeRole => SupaBase.Permissions.Role >= ESupabaseRole.Staff && SupaBase.Permissions.Role > Role;
    
    public SolidColorBrush Brush => new(Role switch
    {
        ESupabaseRole.System => Color.Parse("#B040FF"),
        ESupabaseRole.Owner => Color.Parse("#83c4db"),
        ESupabaseRole.Support => Color.Parse("#635fd4"),
        ESupabaseRole.Staff => Color.Parse("#9856a2"),
        ESupabaseRole.Verified => Color.Parse("#00ff97"),
        ESupabaseRole.User => Colors.White,
        ESupabaseRole.Muted => Color.Parse("#d23940")
    });
    
    [RelayCommand]
    public async Task CopyID()
    {
        await App.Clipboard.SetTextAsync(UserId);
    }
    
    [RelayCommand]
    public async Task SetRole()
    {
        var enumValues = Enum.GetValues<ESupabaseRole>()
            .Where(role => role < SupaBase.Permissions.Role)
            .Select(role => role.Description);
        
        var comboBox = new ComboBox
        {
            ItemsSource = enumValues,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        Info.Dialog($"Set Role for {UserName}", content: comboBox, buttons: 
        [
            new DialogButton
            {
                Text = "Change Role",
                Action = async () =>
                {
                    var role = Enum.GetValues<ESupabaseRole>().FirstOrDefault(role => role.Description.Equals(comboBox.SelectedItem));
                    await SupaBase.Client.Rpc("set_role", new
                    {
                        target_user_id = UserId,
                        new_role = role.ToString().ToLower()
                    });
                }
            }
        ]);
    }
}

public class ChatUserPresence : BasePresence
{
    [JsonProperty("user_id")] public string UserId;
    [JsonProperty("application")] public string Application;
    [JsonProperty("version")] public string Version;
}