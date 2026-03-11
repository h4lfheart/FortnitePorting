using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Radio;
using FortnitePorting.Services;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using MessageData = FortnitePorting.Models.Information.MessageData;

namespace FortnitePorting.ViewModels;

public partial class MusicViewModel : ViewModelBase
{
    [ObservableProperty] private ReadOnlyObservableCollection<MusicPackItem> _activeCollection;
    [ObservableProperty] private string _searchFilter = string.Empty;

    [ObservableProperty] private RadioPlaylist _activePlaylist;
    [ObservableProperty] private ObservableCollection<RadioPlaylist> _playlists = [RadioPlaylist.Default];
    public RadioPlaylist[] CustomPlaylists => Playlists.Where(p => !p.IsDefault).ToArray();

    public readonly ReadOnlyObservableCollection<MusicPackItem> Filtered;
    public readonly ReadOnlyObservableCollection<MusicPackItem> PlaylistMusicPacks;
    public SourceList<MusicPackItem> Source = new();

    private readonly IObservable<Func<MusicPackItem, bool>> RadioSearchFilter;
    private readonly IObservable<Func<MusicPackItem, bool>> RadioPlaylistFilter;

    private readonly string[] IgnoreFilters = ["Random", "TBD", "MusicPack_000_Default"];
    private const string CLASS_NAME = "AthenaMusicPackItemDefinition";

    public MusicViewModel()
    {
        RadioSearchFilter = this.WhenAnyValue(x => x.SearchFilter).Select(CreateSearchFilter);
        
        RadioPlaylistFilter = this.WhenAnyValue(x => x.ActivePlaylist)
            .Select(playlist => playlist is null
                ? Observable.Return<Func<MusicPackItem, bool>>(_ => true)
                : Observable.Merge(
                        Observable.Return(playlist),
                        playlist.MusicIDs
                            .ToObservableChangeSet()
                            .ToCollection()
                            .Select(_ => playlist)
                    )
                    .Select(CreatePlaylistFilter))
            .Switch();

        Source.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Filter(RadioPlaylistFilter)
            .Sort(SortExpressionComparer<MusicPackItem>.Ascending(i => i.Id))
            .Bind(out PlaylistMusicPacks)
            .Filter(RadioSearchFilter)
            .Sort(SortExpressionComparer<MusicPackItem>.Ascending(i => i.Id))
            .Bind(out Filtered)
            .Subscribe();

        ActiveCollection = Filtered;
    }

    public override async Task Initialize()
    {
        await TaskService.RunDispatcherAsync(async () =>
        {
            foreach (var serializeData in AppSettings.Application.Playlists)
                Playlists.Add(await RadioPlaylist.FromSerializeData(serializeData));
        });

        var assets = UEParse.AssetRegistry
            .Where(d => d.AssetClass.Text.Equals(CLASS_NAME))
            .Where(d => !IgnoreFilters.Any(f => d.AssetName.Text.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var asset in assets)
        {
            try
            {
                var musicPack = await UEParse.Provider.SafeLoadPackageObjectAsync(asset.ObjectPath);
                Source.Add(new MusicPackItem(musicPack));
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }
        }
    }

    public override void OnApplicationExit()
    {
        base.OnApplicationExit();
        AppSettings.Application.Playlists = CustomPlaylists.Select(RadioPlaylistSerializeData.FromPlaylist).ToArray();
    }

    public override async Task OnViewOpened() => Discord.Update("Browsing Music");

    [RelayCommand]
    public async Task AddPlaylist() => Playlists.Add(new RadioPlaylist(isDefault: false));

    [RelayCommand]
    public async Task RemovePlaylist(RadioPlaylist? playlist = null)
    {
        var target = playlist ?? ActivePlaylist;
        if (target.IsDefault) return;
        Playlists.Remove(target);
        if (ActivePlaylist == target)
            ActivePlaylist = Playlists.First();
    }

    [RelayCommand]
    public async Task ExportPlaylist()
    {
        if (ActivePlaylist.IsDefault) return;
        if (await App.SaveFileDialog(suggestedFileName: ActivePlaylist.PlaylistName,
                fileTypes: Globals.PlaylistFileType) is not { } path) return;
        path = path.SubstringBeforeLast(".").SubstringBeforeLast(".");
        var data = RadioPlaylistSerializeData.FromPlaylist(ActivePlaylist);
        await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(data));
    }

    [RelayCommand]
    public async Task ImportPlaylist()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.PlaylistFileType) is not { } path) return;
        var data = JsonConvert.DeserializeObject<RadioPlaylistSerializeData>(await File.ReadAllTextAsync(path));
        if (data is null) return;
        Playlists.Add(await RadioPlaylist.FromSerializeData(data));
    }

    [RelayCommand]
    public async Task RenamePlaylist()
    {
        if (ActivePlaylist is null || ActivePlaylist.IsDefault) return;

        await TaskService.RunDispatcherAsync(() =>
        {
            var textBox = new TextBox
            {
                Text = ActivePlaylist.PlaylistName,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            Info.Dialog($"Rename Playlist", content: textBox, buttons:
            [
                new DialogButton
                {
                    Text = "Rename",
                    Action = () =>
                    {
                        if (!string.IsNullOrWhiteSpace(textBox.Text))
                            ActivePlaylist.PlaylistName = textBox.Text;
                    }
                }
            ]);
        });
    }

    private static Func<MusicPackItem, bool> CreateSearchFilter(string filter)
        => item => item.Match(filter);

    private static Func<MusicPackItem, bool> CreatePlaylistFilter(RadioPlaylist playlist)
    {
        if (playlist is null) return _ => true;
        return item => playlist.ContainsID(item.Id);
    }
}