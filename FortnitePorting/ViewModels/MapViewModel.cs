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
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Map;

using FortnitePorting.Services;
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
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(MapTransform))] private Matrix _mapMatrix = Matrix.Identity;
    public MatrixTransform MapTransform => new(MapMatrix);

    public ItemsControl? GridsControl;

    private static MapInfo[] MapInfos =
    [
        // Battle Royale
        new(
            "Asteria",
            "FortniteGame/Content/Athena/Asteria/Maps/Asteria_Terrain",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/T_MiniMap_Mask",
            0.01375f, 132, 140, 90, 12800, true
        ),
        new(
            "Rufus",
            "FortniteGame/Plugins/GameFeatures/Rufus/Content/Game/Athena/Maps/Athena_Terrain",
            "FortniteGame/Plugins/GameFeatures/Rufus/Content/Game/UI/Capture_Iteration_Discovered_Rufus_03",
            "FortniteGame/Content/Athena/UI/Rufus/Rufus_Map_Frosty_PostMask",
            0.0155f, 256, 448, 102, 12800, true
        ),
        new(
            "Helios",
            "FortniteGame/Content/Athena/Helios/Maps/Helios_Terrain",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/T_MiniMap_Mask",
            0.014f, 0, 128, 92, 12800, true
        ),
        new(
            "Apollo_Retro",
            "FortniteGame/Plugins/GameFeatures/Clyde/Content/Apollo_Terrain_Retro",
            "FortniteGame/Content/Athena/Apollo/Maps/Clyde/Textures/Week3_Adjusted",
            "FortniteGame/Content/Athena/Apollo/Maps/Clyde/Textures/T_Clyde_Minimap_PostMask",
            0.032f, -25, 96, 205, 12800, true
        ),
        new(
            "Hermes",
            "FortniteGame/Plugins/GameFeatures/BRMapCh6/Content/Maps/Hermes_Terrain",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/T_MiniMap_Mask",
            0.0146f, -100, -25, 96, 12800, true, false
        ),
        
        // OG
        new(
            "Figment_S01",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S01_Map/Content/Athena_Terrain_S01",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S01_MapUI/Content/MiniMapAthena_S01_New",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S01_MapUI/Content/T_MiniMap_Mask_Figment",
            0.017f, 380, 470, 110, 12800, true
        ),
        new(
            "Figment_S02",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S02_Map/Content/Athena_Terrain_S02",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S02_MapUI/Content/MiniMapAthena_S02_New",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S02_MapUI/Content/T_MiniMap_Mask_Figment",
            0.017f, 380, 470, 110, 12800, true
        ),
        new(
            "Figment_S03",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S03_Map/Content/Athena_Terrain_S03",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S03_MapUI/Content/MiniMapAthena_S03",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S03_MapUI/Content/MiniMapAthena_S03_Mask",
            0.017f, 380, 470, 110, 12800, true
        ),
        new(
            "Figment_S04",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S04_Map/Content/Athena_Terrain_S04",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S04_MapUI/Content/MiniMapAthena_S04",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S04_MapUI/Content/MiniMapAthena_S04_Mask",
            0.017f, 380, 470, 110, 12800, true
        ),
        new(
            "Figment_S05",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S05_Map/Content/Athena_Terrain_S05",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S05_MapUI/Content/MiniMapAthena_S05Temp",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S05_MapUI/Content/MiniMapAthena_S05_MaskTemp",
            0.017f, 330, 450, 110, 12800, true
        ),
        new(
            "Figment_S06",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S06_Map/Content/Athena_Terrain_S06",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S06_MapUI/Content/MiniMapAthena_S06Temp",
            "FortniteGame/Plugins/GameFeatures/Figment/Figment_S06_MapUI/Content/MiniMapAthena_S06_MaskTemp",
            0.017f, 330, 450, 110, 12800, true
        ),
        
        // reload
        new(
            "BlastBerry",
            "/BlastBerryMap/Maps/BlastBerry_Terrain",
            "/BlastBerryMapUI/Minimap/Capture_Iteration_Discovered_BlastBerry",
            "/BlastBerryMapUI/MiniMap/T_MiniMap_Mask",
            0.0235f, -20, 210, 153, 12800, false
        ),
        new(
            "PunchBerry",
            "/632de27e-4506-41f8-532f-93ac01dc10ca/Maps/PunchBerry_Terrain",
            "/BlastBerryMapUI/MiniMap/Discovered_PunchBerry",
            "/BlastBerryMapUI/MiniMap/T_PB_MiniMap_Mask",
            0.0235f, -20, 210, 153, 12800, true
        ),
        new(
            "DashBerry",
            "/f4032749-42c4-7fe9-7fa2-c78076f34f54/DashBerry",
            "/BlastBerryMapUI/MiniMap/Discovered_DashBerry",
            "/BlastBerryMapUI/MiniMap/MMap_DasBerry_Mask",
            0.0235f, -20, 210, 153, 12800, true
        ),
        new(
            "TimberStake",
            "/4c3853ea-4fd0-364f-c3f9-dabd19e43a23/TimberStake",
            "/BlastBerryMapUI/MiniMap/Discovered_TimberStake",
            "/BlastBerryMapUI/MiniMap/MMap_TimberStake_Mask",
            0.027f, 40, 15, 175, 12800, false, false
        ),
        new(
            "SourSpawn",
            "/8b8c126e-4f3a-816e-1d5c-7983eecfa42a/SourSpawn",
            "/BlastBerryMapUI/MiniMap/Discovered_SourSpawn",
            "/BlastBerryMapUI/MiniMap/MMap_SourSpawn_Mask",
            0.023f, 75, 25, 149, 12800, false, false
        ),
        
        // ballistic
        new(
            "FeralCorgi_2Bombsite_Map",
            "/e1729c50-4845-01ba-18da-478919f7de66/Levels/FeralCorgi_2Bombsite_Map",
            "/e1729c50-4845-01ba-18da-478919f7de66/MiniMap/T_KuraiMiniMap_FullAlpha",
            "/e1729c50-4845-01ba-18da-478919f7de66/MiniMap/T_KuraiMiniMap_FullAlpha",
            1, 0, 0, 0, 12800, true,
            SourceName: "Ballistic"
        ),
        MapInfo.CreateNonDisplay("Athena", "FortniteGame/Content/Athena/Maps/Athena_Terrain"),
        MapInfo.CreateNonDisplay("Apollo", "FortniteGame/Content/Athena/Apollo/Maps/Apollo_Terrain")
    ];
    
    
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
        // in-game maps
        foreach (var mapInfo in MapInfos)
        {
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

        foreach (var map in Maps.ToArray())
        {
            try
            {
                await map.Load();
            }
            catch (Exception e)
            {
                Info.Dialog("Map Export", $"Failed to load {map.Info.Name} for export, skipping.");
#if DEBUG
                Log.Error(e.ToString());
#else
                Maps.Remove(map);
#endif
            }
        }
        
    }
    
    [RelayCommand]
    public async Task ReloadMap()
    {
        await SelectedMap.Load();
    }
    
    public override async Task OnViewOpened()
    {
        if (SelectedMap is null) return;
        
        Discord.Update($"Browsing Map: \"{SelectedMap.Info.Name}\"");
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(SelectedMap):
            {
                GridsControl?.InvalidateVisual();
                
                Discord.Update($"Browsing Map: \"{SelectedMap.Info.Name}\"");
                break;
            }
        }
    }
}
