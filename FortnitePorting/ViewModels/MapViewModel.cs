using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.IO;
using CUE4Parse.Utils;
using FortnitePorting.CUE4Parse.Models.Fortnite;
using FortnitePorting.CUE4Parse.Models.Fortnite.GameFeature;
using FortnitePorting.Framework;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Map;

using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Mapster;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class MapViewModel(
    SettingsService settings,
    SupabaseService supabase,
    APIService api,
    CUE4ParseService ueParse,
    InfoService info,
    NavigationService navigation,
    DiscordService discord,
    AppService app) : ViewModelBase, IResettable
{
    [ObservableProperty] private SupabaseService _supaBase = supabase;

    private readonly SettingsService _settings = settings;
    private readonly APIService _api = api;
    private readonly CUE4ParseService _ueParse = ueParse;
    private readonly InfoService _info = info;
    private readonly NavigationService _navigation = navigation;
    private readonly DiscordService _discord = discord;
    private readonly AppService _app = app;
    
    [ObservableProperty] private ObservableCollection<WorldPartitionMap> _maps = [];
    [ObservableProperty] private WorldPartitionMap _selectedMap;
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string? _currentlyLoadingMap;

    [ObservableProperty] private bool _useMapInfoCreator;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _loadedMaps;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _totalMaps = int.MaxValue;
    public string LoadingPercentageText => $"{(LoadedMaps == 0 && TotalMaps == 0 ? 0 : LoadedMaps * 100f / TotalMaps):N0}%";
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(MapTransform))] private Matrix _mapMatrix = Matrix.Identity;
    public MatrixTransform MapTransform => new(MapMatrix);

    public ItemsControl? GridsControl;
    
    public DirectoryInfo MapsFolder => new(Path.Combine(_app.ApplicationDataFolder.FullName, "Maps"));

    private static string[] PluginRemoveList =
    [
        "FMJam",
        "BlastBerry_Terrain",
        "PunchBerry_Terrain",
        "DashBerry",
        "TimberStake",
        "SourSpawn",
        "FeralCorgi_2Bombsite_Map"
    ];

    public void Reset()
    {
        foreach (var map in Maps)
            map.Detach();
        Maps.Clear();
        SelectedMap = null;
        LoadedMaps = 0;
        TotalMaps = int.MaxValue;
        IsLoading = true;
        CurrentlyLoadingMap = null;
        InvalidateInitialization();
    }

    public override async Task Initialize()
    {
        MapsFolder.Create();
        ExportLocation = _settings.Application.DefaultExportLocation;
        await LoadMapsAsync();
    }

    private async Task LoadMapsAsync()
    {
        await TaskService.RunDispatcherAsync(async () =>
        {
            IsLoading = true;
        
            var mapResponse = await _api.FortnitePorting.Maps();
            foreach (var map in mapResponse.Entries)
            {
                var mapInfo = map.Adapt<MapInfo>();
                if (!mapInfo.IsValid()) continue;

                mapInfo.IsPublished = true;
                
                Maps.Add(new WorldPartitionMap(mapInfo));
            }

            foreach (var mapInfo in _settings.Application.LocalMapInfos.ToArray())
            {
                if (!mapInfo.IsValid())
                {
                    _info.Message("Local Map Info", $"Failed to load {mapInfo.Name} due to invalid file paths, removing from local registry.");
                    _settings.Application.LocalMapInfos.RemoveAll(map => ReferenceEquals(map, mapInfo));
                    continue;
                }

                mapInfo.IsPublished = false;
                Maps.Add(new WorldPartitionMap(mapInfo));
            }

            if (_supaBase.Permissions.CanExportUEFN)
            {
                foreach (var mountedVfs in _ueParse.Provider.MountedVfs)
                {
                    if (mountedVfs is not IoStoreReader { Name: "plugin.utoc" } ioStoreReader) continue;

                    var gameFeatureDataFile = ioStoreReader.Files.FirstOrDefault(file => file.Key.EndsWith("GameFeatureData.uasset", StringComparison.OrdinalIgnoreCase));
                    if (gameFeatureDataFile.Value is null) continue;

                    var gameFeatureData = await _ueParse.Provider.SafeLoadPackageObjectAsync<UFortGameFeatureData>(gameFeatureDataFile.Value.PathWithoutExtension);

                    if (gameFeatureData?.ExperienceData?.DefaultMap is not { } defaultMapPath) continue;

                    var defaultMap = await defaultMapPath.LoadAsync();
                    if (PluginRemoveList.Any(item => defaultMap.Name.Contains(item, StringComparison.OrdinalIgnoreCase))) continue;

                    var mapInfo = MapInfo.CreateNonDisplay(defaultMap.Name, defaultMap.GetPathName().SubstringBeforeLast("."));
                
                    Maps.Add(new WorldPartitionMap(mapInfo));
                }
            }
            
            if (Maps.Count == 0)
            {
                _info.Message("No Supported Maps", "Failed to find any supported maps for processing.");
            }

            TotalMaps = Maps.Count;
            foreach (var map in Maps.ToArray())
            {
                LoadedMaps++;
                
                try
                {
                    CurrentlyLoadingMap = map.MapInfo.Name;
                    await map.Load();
                }
                catch (Exception e)
                {
                    _info.Message(map.MapInfo.Name, $"Failed to load {map.MapInfo.Name} for export, skipping.");
#if DEBUG
                    Log.Error(e.ToString());
#else
                    Maps.Remove(map);
#endif
                }
            }

            SelectedMap = Maps.FirstOrDefault();
            
            IsLoading = false;
        });

    }
    
    [RelayCommand]
    public async Task EditorPublish()
    {
        if (!SelectedMap.MapInfo.IsValid())
        {
            _info.Message("Publish Map", "Map information is invalid, ensure all paths exist");
            return;
        }
        
        _info.Dialog("Publish Map", $"Are you sure you would like to publish {SelectedMap.MapInfo.Name}? This will make the map visible for all users.", buttons: [
            new DialogButton
            {
                Text = "Publish",
                Action = () => TaskService.Run(async () =>
                {
                    if (SelectedMap.MapInfo.Id is null)
                        SelectedMap.MapInfo.Id = await _api.FortnitePorting.CreateMap(SelectedMap.MapInfo); 
                    else
                        await _api.FortnitePorting.UpdateMap(SelectedMap.MapInfo);
                    
                    SelectedMap.MapInfo.IsPublished = true;
                    _settings.Application.LocalMapInfos.RemoveAll(map => ReferenceEquals(map, SelectedMap.MapInfo));
                    
                    _info.Message("Publish Map", $"Successfully published {SelectedMap.MapInfo.Name}!");
                })
            }
        ]);
    }

    [RelayCommand]
    public async Task EditorDelete()
    {
        _info.Dialog("Delete Map", $"Are you sure you would like to delete {SelectedMap.MapInfo.Name}? This will remove the map for all users.", buttons: [
            new DialogButton
            {
                Text = "Delete",
                Action = () =>
                {
                    var targetMapInfo = SelectedMap.MapInfo;
                    if (SelectedMap.MapInfo.IsPublished)
                    {
                        TaskService.Run(async () =>
                        {
                            await _api.FortnitePorting.DeleteMap(targetMapInfo.Id);
                        });
                    }
                        
                    Maps.Remove(SelectedMap);
                    SelectedMap = Maps.FirstOrDefault();
                    _settings.Application.LocalMapInfos.RemoveAll(map => ReferenceEquals(map, targetMapInfo));
                    
                    _info.Message("Delete Map", $"Successfully deleted {targetMapInfo.Name}!");
                }
            }
        ]);
    }
    
    [RelayCommand]
    public async Task EditorReload()
    {
        if (!SelectedMap.MapInfo.IsValid())
        {
            _info.Message("Refresh Map", "Map information is invalid, ensure all paths exist");
            return;
        }
        
        await SelectedMap.Refresh();
    }
    
    [RelayCommand]
    public async Task OpenSettings()
    {
        _navigation.App.Open<ExportSettingsView>();
        _navigation.ExportSettings.Open(ExportLocation);
    }
    
    [RelayCommand]
    public async Task SetExportLocation(EExportLocation location)
    {
        ExportLocation = location;
    }
    
    public override async Task OnViewOpened()
    {
        if (SelectedMap is not null)
            _discord.Update($"Browsing Map: \"{SelectedMap.MapInfo.Name}\"");
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(SelectedMap) when SelectedMap is not null:
            {
                GridsControl?.InvalidateVisual();
                
                _discord.Update($"Browsing Map: \"{SelectedMap.MapInfo.Name}\"");
                break;
            }
        }
    }
}
