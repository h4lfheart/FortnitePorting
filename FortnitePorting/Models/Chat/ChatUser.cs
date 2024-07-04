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
using FortnitePorting.Multiplayer.Extensions;
using FortnitePorting.Multiplayer.Models;
using FortnitePorting.Multiplayer.Packet;
using FortnitePorting.Multiplayer.Packet.Owner;
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

    public Guid Guid => Id.ToFpGuid();
    
    public SolidColorBrush Brush => new(Role switch
    {
        ERoleType.System => Color.Parse("#B040FF"),
        ERoleType.SystemExport => Color.Parse("#39fbdc"),
        ERoleType.Owner => Color.Parse("#acd2f5"),
        ERoleType.Staff => Color.Parse("#9856a2"),
        ERoleType.Muted => Color.Parse("#d23940"),
        _ => Colors.White
    });
    
    [RelayCommand]
    public async Task CopyGUID()
    {
        await Clipboard.SetTextAsync(Guid.ToString());
    }
    
    [RelayCommand]
    public async Task CopyID()
    {
        await Clipboard.SetTextAsync(Id);
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

            await GlobalChatService.Send(new MessagePacket(text), new MetadataBuilder()
                .With("Target", Guid));
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
        var dialog = new ContentDialog
        {
            Title = $"Send Export to {DisplayName}",
            CloseButtonText = "Close",
            PrimaryButtonText = "Send"
        };
        
        var inputBox = new TextBox
        {
            Watermark = "Enter a path"
            
        };
        
        dialog.Content = inputBox;
        dialog.PrimaryButtonCommand = new RelayCommand(async () =>
        {
            if (inputBox.Text is not { } text) return;
            
            var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(Exporter.FixPath(text));
            if (asset is null)
            {
                AppVM.Message("Failed to Send Export", $"Could not load \"{text}\"", InfoBarSeverity.Error);
                return;
            }

            await GlobalChatService.Send(new ExportPacket(text), new MetadataBuilder()
                .With("Target", Guid));
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
    public async Task SetRole()
    {
        var enumValues = Enum.GetValues<ERoleType>()
            .Where(role => role < (ChatVM.Permissions.HasFlag(EPermissions.Owner) ? ERoleType.Owner : ERoleType.Staff))
            .Select(role => role.GetDescription());
        
        var comboBox = new ComboBox
        {
            ItemsSource = enumValues,
            SelectedIndex = 0
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Set Role of {DisplayName}",
            Content = comboBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Set",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                await GlobalChatService.Send(new SetRolePacket(Guid, Enum.GetValues<ERoleType>().FirstOrDefault(role => role.GetDescription().Equals(comboBox.SelectedItem))));
            })
        };

        await dialog.ShowAsync();
    }
}