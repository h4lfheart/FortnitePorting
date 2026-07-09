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
using FortnitePorting.Models.API.Requests;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Supabase.Tables;
using Newtonsoft.Json;
using Supabase.Realtime.Models;

namespace FortnitePorting.Models.Chat;

public partial class ChatUser : ObservableObject
{
    [ObservableProperty] private string _userId;
    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _avatarUrl;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private ESupabaseRole _role;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush)), NotifyPropertyChangedFor(nameof(MuteHeader))] private bool _isMuted;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OnlineVersion))] private string _version;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OnlineVersion))] private string _tag;

    public string OnlineVersion => !string.IsNullOrWhiteSpace(Tag) ? $"{Tag} {Version}" : Version;

    public bool CanChangeRole => SupaBase.Permissions.Role >= ESupabaseRole.Staff && SupaBase.Permissions.Role > Role;
    public bool CanMute => SupaBase.Permissions.Role >= ESupabaseRole.Staff && SupaBase.Permissions.Role > Role;
    public string MuteHeader => IsMuted ? "Unmute" : "Mute";

    public SolidColorBrush Brush => new(IsMuted ? Color.Parse("#d23940") : Role switch
    {
        ESupabaseRole.System => Color.Parse("#B040FF"),
        ESupabaseRole.Owner => Color.Parse("#83c4db"),
        ESupabaseRole.Support => Color.Parse("#635fd4"),
        ESupabaseRole.Staff => Color.Parse("#9856a2"),
        ESupabaseRole.Verified => Color.Parse("#00ff97"),
        ESupabaseRole.User => Colors.White
    });
    
    [RelayCommand]
    public async Task CopyID()
    {
        await App.Clipboard.SetTextAsync(UserId);
    }

    [RelayCommand]
    public async Task ToggleMute()
    {
        await Api.FortnitePorting.PatchUserPermissions(UserId, new UserPermissionPatchRequest
        {
            IsMuted = !IsMuted,
        });
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
                    await Api.FortnitePorting.PatchUserPermissions(UserId, new UserPermissionPatchRequest
                    {
                        Role = role,
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
    [JsonProperty("is_typing")] public bool IsTyping;
}
