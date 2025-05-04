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
using FortnitePorting.OnlineServices.Extensions;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Settings;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using Serilog;
using Exporter = FortnitePorting.Export.Exporter;

namespace FortnitePorting.Models.Chat;

public partial class ChatUser : ObservableObject
{
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _profilePictureURL;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Guid))] private string _id;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private ERoleType _role;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OnlineVersion))] private string _version;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OnlineVersion))] private string _tag;

    public string OnlineVersion => !string.IsNullOrWhiteSpace(Tag) ? $"{Tag} {Version}" : Version;

    public Guid Guid => Id.ToFpGuid();
    
    public SolidColorBrush Brush => new(Role switch
    {
        ERoleType.System => Color.Parse("#B040FF"),
        ERoleType.SystemExport => Color.Parse("#39fbdc"),
        ERoleType.Owner => Color.Parse("#acd2f5"),
        ERoleType.Staff => Color.Parse("#9856a2"),
        ERoleType.Muted => Color.Parse("#d23940"),
        ERoleType.Trusted => Color.Parse("#00ff97"),
        ERoleType.Verified => Color.Parse("#e91c63"),
        _ => Colors.White
    });
    
    [RelayCommand]
    public async Task CopyGUID()
    {
        await App.Clipboard.SetTextAsync(Guid.ToString());
    }
    
    [RelayCommand]
    public async Task CopyID()
    {
        await App.Clipboard.SetTextAsync(Id);
    }
    
    [RelayCommand]
    public async Task SendMessage()
    {
        var dialog = new ContentDialog
        {
            Title = $"Send Message to {DisplayName}",
            CloseButtonText = "Close",
            PrimaryButtonText = "Send"
        };
        
        var inputBox = new TextBox
        {
            Watermark = "Enter a message"
            
        };
        
        dialog.Content = inputBox;
        dialog.PrimaryButtonCommand = new RelayCommand(async () =>
        {
            if (inputBox.Text is not { } text) return;

        });
        
        inputBox.AddHandler(InputElement.KeyDownEvent, (sender, args) =>
        {
            if (args.Key != Key.Enter) return;
            
            dialog.PrimaryButtonCommand.Execute(null);
            dialog.Hide();
        }, RoutingStrategies.Tunnel);
        

        await dialog.ShowAsync();
    }
    
    [RelayCommand]
    public async Task SendExport()
    {
        var xaml =
            """
                <ContentControl xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:ext="clr-namespace:FortnitePorting.Shared.Extensions;assembly=FortnitePorting.Shared"
                            xmlns:shared="clr-namespace:FortnitePorting.Shared;assembly=FortnitePorting.Shared">
                    <StackPanel HorizontalAlignment="Stretch">
                        <TextBox x:Name="InputBox" Watermark="File Path" TextWrapping="Wrap"/>
                        <TextBox x:Name="MessageBox" Watermark="Message (Optional)" TextWrapping="Wrap" Margin="{ext:Space 0, 1, 0, 0}"/>
                    </StackPanel>
                </ContentControl>
            """;
                    
        var content = xaml.CreateXaml<ContentControl>(new
        {
            Users = ChatVM.Users.Select(user => user.DisplayName)
        });
                    
        var inputBox = content.FindControl<TextBox>("InputBox");
        var messageBox = content.FindControl<TextBox>("MessageBox");
        
        var dialog = new ContentDialog
        {
            Title = $"Send Export to {DisplayName}",
            CloseButtonText = "Close",
            PrimaryButtonText = "Send",
            Content = content,
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (inputBox?.Text is not { } text) return;
                if (messageBox?.Text is not { } message) return;

                var path = Exporter.FixPath(text);
                var asset = await UEParse.Provider.SafeLoadPackageObjectAsync(path);
                if (asset is null)
                {
                    Info.Message("Failed to Send Export", $"Could not load \"{text}\"", InfoBarSeverity.Error);
                    return;
                }

            })
        };

        await dialog.ShowAsync();
    }
    
    [RelayCommand]
    public async Task SetRole()
    {
        var enumValues = Enum.GetValues<ERoleType>()
            .Where(role => role < (ChatVM.Permissions.HasFlag(EPermissions.Owner) ? ERoleType.Owner : ERoleType.Staff))
            .Select(role => role.GetDescription());
        
        var comboBox = new ComboBox
        {
            ItemsSource = enumValues,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Set Role of {DisplayName}",
            Content = comboBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Set",
        };

        await dialog.ShowAsync();
    }
}