using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using Globals = FortnitePorting.Globals;

namespace FortnitePorting.Models.Files;

public partial class FlatItem : ObservableObject
{
    [ObservableProperty] private string _path;

    public FlatItem(string path)
    {
        Path = path;
    }

    [RelayCommand]
    public async Task CopyPath()
    {
        await App.Clipboard.SetTextAsync(Path);
    }
    
    [RelayCommand]
    public async Task SendToUser()
    {
        var xaml =
            """
                <ContentControl xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:ext="clr-namespace:FortnitePorting.Shared.Extensions;assembly=FortnitePorting.Shared"
                            xmlns:shared="clr-namespace:FortnitePorting.Shared;assembly=FortnitePorting.Shared">
                    <StackPanel HorizontalAlignment="Stretch">
                        <ComboBox x:Name="UserSelectionBox" SelectedIndex="0" Margin="{ext:Space 0, 1, 0, 0}"
                                  ItemsSource="{Binding Users}"
                                  HorizontalAlignment="Stretch">
                            <ComboBox.ItemContainerTheme>
                                <ControlTheme x:DataType="ext:EnumRecord" TargetType="ComboBoxItem" BasedOn="{StaticResource {x:Type ComboBoxItem}}">
                                    <Setter Property="IsEnabled" Value="{Binding !IsDisabled}"/>
                                </ControlTheme>
                            </ComboBox.ItemContainerTheme>
                        </ComboBox>
                        <TextBox x:Name="MessageBox" Watermark="Message (Optional)" TextWrapping="Wrap" Margin="{ext:Space 0, 1, 0, 0}"/>
                    </StackPanel>
                </ContentControl>
            """;
                    
        var content = xaml.CreateXaml<ContentControl>(new
        {
            Users = ChatVM.Users.Select(user => user.DisplayName)
        });
        
        var comboBox = content.FindControl<ComboBox>("UserSelectionBox");
        comboBox.SelectedIndex = 0;
        var messageBox = content.FindControl<TextBox>("MessageBox");
        
        var name = Path.SubstringAfterLast("/").SubstringBefore(".");
        var dialog = new ContentDialog
        {
            Title = $"Export \"{name}\" to User",
            Content = content,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Send",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (messageBox?.Text is not { } message) return;
                
                var targetUser = ChatVM.Users.FirstOrDefault(user => user.DisplayName.Equals(comboBox.SelectionBoxItem));
                if (targetUser is null) return;
                
                Info.Message("Export Sent", $"Successfully sent \"{name}\" to {targetUser.DisplayName}");
            })
        };

        await dialog.ShowAsync();
    }
    
    [RelayCommand]
    public async Task CopyProperties()
    {
        var assets = await UEParse.Provider.LoadAllObjectsAsync(Exporter.FixPath(Path));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        await App.Clipboard.SetTextAsync(json);
    }
    
    [RelayCommand]
    public async Task SaveProperties()
    {
        if (await App.SaveFileDialog(suggestedFileName: Path.SubstringAfterLast("/").SubstringBefore("."),
                Globals.JSONFileType) is { } path)
        {
            var assets = await UEParse.Provider.LoadAllObjectsAsync(Exporter.FixPath(Path));
            var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
            await File.WriteAllTextAsync(path, json);
        }
    }
}