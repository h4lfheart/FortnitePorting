using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Export;
using FortnitePorting.Multiplayer.Models;
using FortnitePorting.Multiplayer.Packet;
using FortnitePorting.Multiplayer.Packet.Owner;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Files;

public partial class FlatViewItem : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _path;

    public FlatViewItem(int id, string path)
    {
        Id = id;
        Path = path;
    }

    [RelayCommand]
    public async Task CopyPath()
    {
        await Clipboard.SetTextAsync(Path);
    }
    
    [RelayCommand]
    public async Task SendToUser()
    {
        var users = ChatVM.Users.Select(user => user.DisplayName);
        var comboBox = new ComboBox
        {
            ItemsSource = users,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var name = Path.SubstringAfterLast("/").SubstringBefore(".");
        var dialog = new ContentDialog
        {
            Title = $"Export \"{name}\" to User",
            Content = comboBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Send",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                var targetUser = ChatVM.Users.FirstOrDefault(user => user.DisplayName.Equals(comboBox.SelectionBoxItem));
                if (targetUser is null) return;
                
                await GlobalChatService.Send(new ExportPacket(Exporter.FixPath(Path)), new MetadataBuilder().With("Target", targetUser.Guid));
                AppWM.Message("Export Sent", $"Successfully sent \"{name}\" to {targetUser.DisplayName}");
            })
        };

        await dialog.ShowAsync();
    }
}