using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Export;
using FortnitePorting.Multiplayer.Data;
using FortnitePorting.Services;

namespace FortnitePorting.Models.Chat;

public partial class ChatUser : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private string _name;
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _avatar;
    
    public SolidColorBrush Brush => Name switch
    {
        "SYSTEM" => new SolidColorBrush(Color.Parse("#B040FF")),
        "EXPORT" => new SolidColorBrush(Color.Parse("#39fbdc")),
        "Half" => new SolidColorBrush(Color.Parse("#10acff")),
        _ => new SolidColorBrush(Colors.White)
    };

    [RelayCommand]
    public async Task SendExport()
    {
        var inputBox = new TextBox
        {
            Watermark = "Enter a path to export"
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Send Export to {Name}",
            Content = inputBox,
            CloseButtonText = "Close",
            PrimaryButtonText = "Send",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (inputBox.Text is not { } text) return;
                
                await SocketService.Send(new ExportData(Name, Exporter.FixPath(text)));
            })
        };

        await dialog.ShowAsync();
    }
    
    [RelayCommand]
    public async Task SendMessage()
    {
        var inputBox = new TextBox
        {
            Watermark = "Enter a messages"
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Send Message to {Name}",
            Content = inputBox,
            CloseButtonText = "Close",
            PrimaryButtonText = "Send",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (inputBox.Text is not { } text) return;
                
                await SocketService.Send(new DirectMessageData(Name, text));
            })
        };

        await dialog.ShowAsync();
    }
    
    [RelayCommand]
    public async Task CopyID()
    {
        await Clipboard.SetTextAsync(Id.ToString());
    }
}
