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
using FortnitePorting.Models.Map;

using FortnitePorting.Services;
using FortnitePorting.Views;
using Mapster;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class MapViewModel : ViewModelBase
{
    [ObservableProperty] private SettingsService _appSettings;

    public MapViewModel(SettingsService settings)
    {
        AppSettings = settings;
    }
    
    [ObservableProperty] private ObservableCollection<WorldPartitionMap> _maps = [];
    [ObservableProperty] private WorldPartitionMap _selectedMap;
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _currentlyLoadingMap;
    
    
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

                    var mapInfo = MapInfo.CreateNonDisplay(defaultMap.Name, defaultMap.GetPathName().SubstringBeforeLast("."), sourceName: "UEFN");
                
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
    public async Task ReloadMap()
    {
        await SelectedMap.Load();
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
