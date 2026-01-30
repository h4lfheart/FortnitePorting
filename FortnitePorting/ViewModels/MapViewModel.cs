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
using FortnitePorting.Framework;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Map;

using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Mapster;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class MapViewModel : ViewModelBase
{
    [ObservableProperty] private SettingsService _appSettings;
    [ObservableProperty] private SupabaseService _supaBase;

    public MapViewModel(SettingsService settings, SupabaseService supabase)
    {
        AppSettings = settings;
        SupaBase = supabase;
    }
    
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
    
    public DirectoryInfo MapsFolder => new(Path.Combine(App.ApplicationDataFolder.FullName, "Maps"));

    public MapViewModel()
    {
        MapsFolder.Create();
    }

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

    public override async Task Initialize()
    {
        await TaskService.RunDispatcherAsync(async () =>
        {
             IsLoading = true;
        
            var maps = await Api.FortnitePorting.Maps();
            foreach (var map in maps)
            {
                var mapInfo = map.Adapt<MapInfo>();
                if (!mapInfo.IsValid()) continue;

                mapInfo.IsPublished = true;
                
                Maps.Add(new WorldPartitionMap(mapInfo));
            }

            foreach (var mapInfo in AppSettings.Application.LocalMapInfos.ToArray())
            {
                if (!mapInfo.IsValid())
                {
                    Info.Message("Local Map Info", $"Failed to load {mapInfo.Id} due to invalid file paths, removing from local registry.");
                    AppSettings.Application.LocalMapInfos.RemoveAll(map => map.Id.Equals(mapInfo.Id));
                    continue;
                }

                mapInfo.IsPublished = false;
                Maps.Add(new WorldPartitionMap(mapInfo));
            }

            if (SupaBase.Permissions.CanExportUEFN)
            {
                foreach (var mountedVfs in UEParse.Provider.MountedVfs)
                {
                    if (mountedVfs is not IoStoreReader { Name: "plugin.utoc" } ioStoreReader) continue;

                    var gameFeatureDataFile = ioStoreReader.Files.FirstOrDefault(file => file.Key.EndsWith("GameFeatureData.uasset", StringComparison.OrdinalIgnoreCase));
                    if (gameFeatureDataFile.Value is null) continue;

                    var gameFeatureData = await UEParse.Provider.SafeLoadPackageObjectAsync<UFortGameFeatureData>(gameFeatureDataFile.Value.PathWithoutExtension);

                    if (gameFeatureData?.ExperienceData?.DefaultMap is not { } defaultMapPath) continue;

                    var defaultMap = await defaultMapPath.LoadAsync();
                    if (PluginRemoveList.Any(item => defaultMap.Name.Contains(item, StringComparison.OrdinalIgnoreCase))) continue;

                    var mapInfo = MapInfo.CreateNonDisplay(defaultMap.Name, defaultMap.GetPathName().SubstringBeforeLast("."));
                
                    Maps.Add(new WorldPartitionMap(mapInfo));
                }
            }
            
            if (Maps.Count == 0)
            {
                Info.Message("No Supported Maps", "Failed to find any supported maps for processing.");
            }

            TotalMaps = Maps.Count;
            foreach (var map in Maps.ToArray())
            {
                LoadedMaps++;
                
                try
                {
                    CurrentlyLoadingMap = map.MapInfo.Id;
                    await map.Load();
                }
                catch (Exception e)
                {
                    Info.Message(map.MapInfo.Id, $"Failed to load {map.MapInfo.Id} for export, skipping.");
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
            Info.Message("Publish Map", "Map information is invalid, ensure all paths exist");
            return;
        }
        
        Info.Dialog("Publish Map", $"Are you sure you would like to publish {SelectedMap.MapInfo.Id}? This will make the map visible for all users.", buttons: [
            new DialogButton
            {
                Text = "Publish",
                Action = () => TaskService.Run(async () =>
                {
                    await Api.FortnitePorting.PostMap(SelectedMap.MapInfo);
                    SelectedMap.MapInfo.IsPublished = true;
                    AppSettings.Application.LocalMapInfos.RemoveAll(map => map.Id.Equals(SelectedMap.MapInfo.Id));
                    
                    Info.Message("Publish Map", $"Successfully published {SelectedMap.MapInfo.Id}!");
                })
            }
        ]);
    }

    [RelayCommand]
    public async Task EditorDelete()
    {
        Info.Dialog("Delete Map", $"Are you sure you would like to delete {SelectedMap.MapInfo.Id}? This will remove the map for all users.", buttons: [
            new DialogButton
            {
                Text = "Delete",
                Action = () =>
                {
                    var targetId = SelectedMap.MapInfo.Id;
                    if (SelectedMap.MapInfo.IsPublished)
                    {
                        TaskService.Run(async () =>
                        {
                            await Api.FortnitePorting.DeleteMap(targetId);
                        });
                    }
                        
                    Maps.Remove(SelectedMap);
                    SelectedMap = Maps.FirstOrDefault();
                    AppSettings.Application.LocalMapInfos.RemoveAll(map => map.Id.Equals(targetId));
                    
                    Info.Message("Delete Map", $"Successfully deleted {targetId}!");
                }
            }
        ]);
    }
    
    [RelayCommand]
    public async Task EditorReload()
    {
        if (!SelectedMap.MapInfo.IsValid())
        {
            Info.Message("Refresh Map", "Map information is invalid, ensure all paths exist");
            return;
        }
        
        await SelectedMap.Refresh();
    }
    
    [RelayCommand]
    public async Task OpenSettings()
    {
        Navigation.App.Open<ExportSettingsView>();
        Navigation.ExportSettings.Open(ExportLocation);
    }
    
    [RelayCommand]
    public async Task SetExportLocation(EExportLocation location)
    {
        ExportLocation = location;
    }
    
    public override async Task OnViewOpened()
    {
        if (SelectedMap is null) return;
        
        Discord.Update($"Browsing Map: \"{SelectedMap.MapInfo.Id}\"");
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(SelectedMap):
            {
                GridsControl?.InvalidateVisual();
                
                Discord.Update($"Browsing Map: \"{SelectedMap.MapInfo.Id}\"");
                break;
            }
        }
    }
}
