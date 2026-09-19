using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Chat;
using FortnitePorting.Services;
using FortnitePorting.Views;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Files;

public partial class FlatItem : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ResolvedDisplayName))]
    private string _path;

    [ObservableProperty] private string _vfsName;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ResolvedDisplayName))]
    private string? _displayName;

    [ObservableProperty] private string? _exportType;
    [ObservableProperty] private Bitmap? _previewIcon;

    public string ResolvedDisplayName => !string.IsNullOrEmpty(DisplayName)
        ? DisplayName
        : Path.SubstringAfterLast("/").SubstringBefore(".");

    private readonly SemaphoreSlim _previewLock = new(1, 1);
    private bool _previewLoaded;

    public FlatItem(string path, string vfsName = "")
    {
        Path = path;
        VfsName = vfsName;
    }

    public async Task LoadPreviewAsync()
    {
        if (_previewLoaded) return;

        await _previewLock.WaitAsync();
        try
        {
            if (_previewLoaded) return;

            var (icon, displayName, exportType) = await UEParse.ResolveGameFileAsync(Path);
            await TaskService.RunDispatcherAsync(() =>
            {
                PreviewIcon = icon;
                DisplayName = displayName;
                ExportType = exportType;
            });
            _previewLoaded = true;
        }
        finally
        {
            _previewLock.Release();
        }
    }

    [RelayCommand]
    public async Task CopyPath(bool withoutExtension = false)
    {
        await App.Clipboard.SetTextAsync(withoutExtension ? Path.SubstringBefore(".") : Path);
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

    [RelayCommand]
    public async Task SendToChat()
    {
        var (icon, displayName, _) = await UEParse.ResolveGameFileAsync(Path);
        await TaskService.RunDispatcherAsync(() =>
        {
            ChatVM.PendingGameFile = new PendingGameFileAttachment(Path, icon, displayName);
            Navigation.App.Open<ChatView>();
        });
    }
}