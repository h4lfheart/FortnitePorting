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
using FortnitePorting.Export;
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
        await Clipboard.SetTextAsync(Path);
    }
    
    [RelayCommand]
    public async Task CopyProperties()
    {
        var assets = await CUE4ParseVM.Provider.LoadAllObjectsAsync(Exporter.FixPath(Path));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        await Clipboard.SetTextAsync(json);
    }
    
    [RelayCommand]
    public async Task SaveProperties()
    {
        if (await SaveFileDialog(suggestedFileName: Path.SubstringAfterLast("/").SubstringBefore("."),
                Globals.JSONFileType) is { } path)
        {
            var assets = await CUE4ParseVM.Provider.LoadAllObjectsAsync(Exporter.FixPath(Path));
            var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
            await File.WriteAllTextAsync(path, json);
        }
    }
}