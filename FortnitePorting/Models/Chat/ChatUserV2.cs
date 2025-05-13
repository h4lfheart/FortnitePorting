using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.OnlineServices.Extensions;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Settings;
using Mapster;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using Serilog;
using Supabase.Realtime.Models;
using Exporter = FortnitePorting.Exporting.Exporter;

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
        ESupabaseRole.Owner => Color.Parse("#acd2f5"),
        ESupabaseRole.Staff => Color.Parse("#9856a2"),
        ESupabaseRole.Verified => Color.Parse("#e91c63"),
        ESupabaseRole.User => Colors.White
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
            .Select(role => role.GetDescription());
        
        var comboBox = new ComboBox
        {
            ItemsSource = enumValues,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Set Role of {UserName}",
            Content = comboBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Set",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                var role = Enum.GetValues<ESupabaseRole>().FirstOrDefault(role => role.GetDescription().Equals(comboBox.SelectedItem));
                await SupaBase.Client.Rpc("set_role", new
                {
                    target_user_id = UserId,
                    new_role = role.ToString().ToLower()
                });
            })
        };

        await dialog.ShowAsync();
    }
}

public class ChatUserPresence : BasePresence
{
    [JsonProperty("application")] public string Application;
    [JsonProperty("version")] public string Version;
}