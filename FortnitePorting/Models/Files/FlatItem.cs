using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using Newtonsoft.Json;

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