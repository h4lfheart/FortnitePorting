using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Windows;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Exporter = FortnitePorting.Export.Exporter;
using ImageExtensions = FortnitePorting.Shared.Extensions.ImageExtensions;
using Vector2 = System.Numerics.Vector2;

namespace FortnitePorting.ViewModels;

public partial class MapViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<WorldPartitionMap> _maps = [];
    [ObservableProperty] private WorldPartitionMap _selectedMap;
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
    
    public bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    public static MapInfo[] MapInfos =
    [
        new(
            "Helios",
            "FortniteGame/Content/Athena/Helios/Maps/Helios_Terrain",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap",
            "FortniteGame/Content/Athena/Apollo/Maps/UI/T_MiniMap_Mask",
            0.014f, 0, 128, 92, 12800, true
        ),
        new(
            "Rufus",
            "FortniteGame/Plugins/GameFeatures/Rufus/Content/Game/Athena/Maps/Athena_Terrain",
            "FortniteGame/Plugins/GameFeatures/Rufus/Content/Game/UI/Capture_Iteration_Discovered_Rufus_03",
            "FortniteGame/Content/Athena/UI/Rufus/Rufus_Map_Frosty_PostMask",
            0.0155f, 256, 448, 102, 12800, true
        ),
        new(
            "Apollo_Retro",
            "FortniteGame/Plugins/GameFeatures/Clyde/Content/Apollo_Terrain_Retro",
            "FortniteGame/Content/Athena/Apollo/Maps/Clyde/Textures/Week1_Adjusted",
            "FortniteGame/Content/Athena/Apollo/Maps/Clyde/Textures/T_Clyde_Minimap_PostMask",
            0.032f, -25, 96, 205, 12800, true
        ),
        new(
            "BlastBerry",
            "/BlastBerryMap/Maps/BlastBerry_Terrain",
            "/BlastBerry/Minimap/Capture_Iteration_Discovered_BlastBerry",
            "/BlastBerry/MiniMap/T_MiniMap_Mask",
            0.046f, 32, 168, 300, 12800, false
        ),
        new(
            "PunchBerry",
            "FortniteGame/Plugins/GameFeatures/632de27e-4506-41f8-532f-93ac01dc10ca/Content/Maps/PunchBerry_Terrain",
            "FortniteGame/Plugins/GameFeatures/BlastBerry/Content/MiniMap/Discovered_PunchBerry",
            "FortniteGame/Plugins/GameFeatures/BlastBerry/Content/MiniMap/T_PB_MiniMap_Mask",
            0.0475f, 0, 384, 305, 12800, true
        ),
        MapInfo.CreateNonDisplay("Athena", "FortniteGame/Content/Athena/Maps/Athena_Terrain"),
        MapInfo.CreateNonDisplay("Apollo", "FortniteGame/Content/Athena/Apollo/Maps/Apollo_Terrain")
    ];

    private static string[] PluginRemoveList =
    [
        "FMJam_",
        "PunchBerry_Terrain"
    ];

    public override async Task Initialize()
    {
        // in-game maps
        foreach (var mapInfo in MapInfos)
        {
            if (!mapInfo.IsValid()) continue;
            
            Maps.Add(new WorldPartitionMap(mapInfo));
        }
        
        // uefn maps
        foreach (var mountedVfs in CUE4ParseVM.Provider.MountedVfs)
        {
            if (mountedVfs is not IoStoreReader { Name: "plugin.utoc" } ioStoreReader) continue;

            var gameFeatureDataFile = ioStoreReader.Files.FirstOrDefault(file => file.Key.EndsWith("GameFeatureData.uasset", StringComparison.OrdinalIgnoreCase));
            if (gameFeatureDataFile.Value is null) continue;

            var gameFeatureData = await CUE4ParseVM.Provider.TryLoadObjectAsync<UFortGameFeatureData>(gameFeatureDataFile.Value.PathWithoutExtension);

            if (gameFeatureData?.ExperienceData?.DefaultMap is not { } defaultMapPath) continue;

            var defaultMap = await defaultMapPath.LoadAsync();
            if (PluginRemoveList.Any(item => defaultMap.Name.Contains(item, StringComparison.OrdinalIgnoreCase))) continue;

            var mapInfo = MapInfo.CreateNonDisplay(defaultMap.Name, defaultMap.GetPathName().SubstringBeforeLast("."));
            
            Maps.Add(new WorldPartitionMap(mapInfo));
        }

        if (Maps.Count == 0)
        {
            AppWM.Dialog("No Supported Maps", "Failed to find any supported maps for processing.");
        }

        foreach (var map in Maps)
        {
            await map.Load();
        }
        
    }
    
    [RelayCommand]
    public async Task ReloadMap()
    {
        await SelectedMap.Load();
    }
    
    public override async Task OnViewOpened()
    {
        DiscordService.Update($"Browsing Map: \"{SelectedMap.Info.Name}\"", "Map");
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(SelectedMap):
            {
                DiscordService.Update($"Browsing Map: \"{SelectedMap.Info.Name}\"", "Map");
                break;
            }
        }
    }
}

public partial class WorldPartitionMap : ObservableObject
{
    [ObservableProperty] private MapInfo _info;
    [ObservableProperty] private Bitmap _mapBitmap = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/Transparent1x1.png");
    [ObservableProperty] private Bitmap _maskBitmap;
    [ObservableProperty] private string _worldName = string.Empty;
    [ObservableProperty] private bool _dataLoaded = false;
    
    [ObservableProperty] private ObservableCollection<WorldPartitionGridMap> _selectedMaps = [];
    
    [ObservableProperty] private ObservableCollection<WorldPartitionGrid> _grids = [];
    
    public int GridCount => Grids.Sum(grid => grid.Maps.Count);
    
    private string ExportPath => Path.Combine(MapsFolder.FullName, WorldName);
    
    private UWorld _world;
    private ULevel _level;
    
    private static readonly Color HeightBaseColor = Color.FromRgb(0x79, 0x79, 0x79);
    private static readonly Color NormalBaseColor = Color.FromRgb(0x7f, 0x7f, 0xFF);

    public WorldPartitionMap(MapInfo info)
    {
        Info = info;
        
        Grids.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(GridCount));
    }

    public async Task Load()
    {
        Grids = [];
        
        if (!Info.IsNonDisplay)
        {
            var maskTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(Info.UseMask ? Info.MaskPath : "FortniteGame/Content/Global/Textures/Default/Blanks/T_White");
            MaskBitmap = maskTexture.Decode()!.ToWriteableBitmap();
        
            var mapTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(Info.MinimapPath);
            MapBitmap = mapTexture.Decode()!.ToWriteableBitmap();
        }
        
        WorldName = Info.MapPath.SubstringAfterLast("/");
        
        _world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(Info.MapPath);
        _level = await _world.PersistentLevel.LoadAsync<ULevel>();

        if (_level.GetOrDefault<UObject>("WorldSettings") is { } worldSettings
            && worldSettings.GetOrDefault<UObject>("WorldPartition") is { } worldPartition
            && worldPartition.GetOrDefault<UObject>("RuntimeHash") is { } runtimeHash)
        {
            foreach (var streamingData in runtimeHash.GetOrDefault("RuntimeStreamingData", Array.Empty<FStructFallback>()))
            {
                var cells = new List<FPackageIndex>();
                cells.AddRange(streamingData.GetOrDefault("SpatiallyLoadedCells", Array.Empty<FPackageIndex>()));
                cells.AddRange(streamingData.GetOrDefault("NonSpatiallyLoadedCells", Array.Empty<FPackageIndex>()));

                foreach (var cell in cells)
                {
                    var gridCell = await cell.LoadAsync();
                    if (gridCell is null) continue;
                
                    var levelStreaming = gridCell.GetOrDefault<UObject?>("LevelStreaming");
                    if (levelStreaming is null) continue;

                    var position = FVector.ZeroVector;
                    var runtimeCellData = gridCell.Get<UObject>("RuntimeCellData");
                    var boundsProperty = runtimeCellData.GetOrDefault<StructProperty?>("CellBounds");
                    if (boundsProperty?.GetValue<FBox>() is { } bounds)
                    {
                        position = bounds.GetCenter();

                        // please don't break other maps
                        position.X -= position.X % Info.MinGridDistance;
                        position.Y -= position.Y % Info.MinGridDistance;
                    }

                
                    var worldAsset = levelStreaming.Get<FSoftObjectPath>("WorldAsset");
                    if (Grids.FirstOrDefault(grid => grid.OriginalPosition == position) is { } targetGrid)
                    {
                        targetGrid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                    }
                    else
                    {
                        var grid = new WorldPartitionGrid(position, Info);
                        grid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                        Grids.Add(grid);
                    }
                }
            }
            
            foreach (var streamingGrid in runtimeHash.GetOrDefault("StreamingGrids", Array.Empty<FStructFallback>()))
            {
                foreach (var gridLevel in streamingGrid.GetOrDefault("GridLevels", Array.Empty<FStructFallback>()))
                foreach (var layerCell in gridLevel.GetOrDefault("LayerCells", Array.Empty<FStructFallback>()))
                foreach (var gridCell in layerCell.GetOrDefault("GridCells", Array.Empty<UObject>()))
                {
                    var levelStreaming = gridCell.GetOrDefault<UObject?>("LevelStreaming");
                    if (levelStreaming is null) continue;

                    var worldAsset = levelStreaming.Get<FSoftObjectPath>("WorldAsset");

                    var runtimeCellData = gridCell.Get<UObject>("RuntimeCellData");
                    var position = runtimeCellData.GetOrDefault<FVector>("Position");

                    if (Grids.FirstOrDefault(grid => grid.OriginalPosition == position) is { } targetGrid)
                    {
                        targetGrid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                    }
                    else
                    {
                        var grid = new WorldPartitionGrid(position, Info);
                        grid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                        Grids.Add(grid);
                    }
                }
            }

           
        }
        
        Directory.CreateDirectory(ExportPath);
        DataLoaded = true;
    }

    public void ClearSelectedMaps()
    {
        Grids.ForEach(grid => grid.IsSelected = false);
        SelectedMaps.Clear();
    }
    
    [RelayCommand]
    public async Task ExportMinimap()
    {
        MapBitmap.Save(GetExportPath($"Minimap_{WorldName}"));
        MaskBitmap.Save(GetExportPath($"Minimap_Mask_{WorldName}"));
        Launch(ExportPath);
    }
    
    [RelayCommand]
    public async Task ExportHeight()
    {
        await ExportLandscapeMaps(EMapTextureExportType.Height);
    }
    
    [RelayCommand]
    public async Task ExportNormal()
    {
        await ExportLandscapeMaps(EMapTextureExportType.Normal);
    }
    
    [RelayCommand]
    public async Task ExportWeight()
    {
        await ExportLandscapeMaps(EMapTextureExportType.Weight);
    }
    
    [RelayCommand]
    public async Task ExportLandscape()
    {
        var meta = AppSettings.Current.CreateExportMeta(MapVM.ExportLocation);
        meta.WorldFlags = EWorldFlags.Landscape;
        await Exporter.Export(_world, EExportType.World, meta);
    }
    
    [RelayCommand]
    public async Task ExportActors()
    {
        var meta = AppSettings.Current.CreateExportMeta(MapVM.ExportLocation);
        meta.WorldFlags = EWorldFlags.Actors;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;
        await Exporter.Export(_world, EExportType.World, meta);
    }
    
    [RelayCommand]
    public async Task ExportWorldPartitionActors()
    {
        var meta = AppSettings.Current.CreateExportMeta(MapVM.ExportLocation);
        meta.WorldFlags = EWorldFlags.WorldPartitionGrids;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;
        await Exporter.Export(_world, EExportType.World, meta);
    }
    
    [RelayCommand]
    public async Task ExportFullMap()
    {
        var meta = AppSettings.Current.CreateExportMeta(MapVM.ExportLocation);
        meta.WorldFlags = EWorldFlags.Actors | EWorldFlags.Landscape | EWorldFlags.WorldPartitionGrids;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;
        await Exporter.Export(_world, EExportType.World, meta);
    }
    
    [RelayCommand]
    public async Task CopyIDs()
    {
        await Clipboard.SetTextAsync(string.Join('\n', SelectedMaps.Select(map => map.Name)));
    }
    
    [RelayCommand]
    public async Task Preview()
    {
        var levels = new List<UObject>();
        foreach (var map in SelectedMaps)
        {
            var world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(map.Path);
            var level = await world.PersistentLevel.LoadAsync<ULevel>();
            levels.Add(level);
        }
        
        ModelPreviewWindow.Preview(levels);
        ClearSelectedMaps();
    }
    
    [RelayCommand]
    public async Task Export()
    {
        var worlds = new List<UObject>();
        foreach (var map in SelectedMaps)
        {
            var world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(map.Path);
            worlds.Add(world);
        }

        var meta = AppSettings.Current.CreateExportMeta(MapVM.ExportLocation);
        meta.WorldFlags = EWorldFlags.Actors | EWorldFlags.Landscape | EWorldFlags.WorldPartitionGrids;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;
        await Exporter.Export(worlds, EExportType.World, meta);
        
        if (AppSettings.Current.Online.UseIntegration)
        {
            var exports = SelectedMaps.Select(map => new PersonalExport(map.Path));
            await ApiVM.FortnitePorting.PostExportsAsync(exports);
        }
        
        ClearSelectedMaps();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Grids.Clear();
        ClearSelectedMaps();
        await Load();
    }

    private async Task ExportLandscapeMaps(EMapTextureExportType exportType)
    {
        var heightTileInfos = new List<MapTextureTileInfo>();
        var weightmapTileInfos = new Dictionary<string, List<MapTextureTileInfo>>();
        
        async Task<int> CollectTileInfos(ULevel level)
        {
            var proxyDetectedCount = 0;
            foreach (var actor in level.Actors)
            {
                if (!actor.Name.StartsWith("LandscapeStreamingProxy")) continue;

                var landscapeStreamingProxy = await actor.LoadAsync();
                if (landscapeStreamingProxy is null) continue;

                proxyDetectedCount++;
                
                var landscapeComponents = landscapeStreamingProxy.GetOrDefault("LandscapeComponents", Array.Empty<UObject>());
                foreach (var landscapeComponent in landscapeComponents)
                {
                    var x = landscapeComponent.GetOrDefault<int>("SectionBaseX");
                    var y = landscapeComponent.GetOrDefault<int>("SectionBaseY");
                    
                    switch (exportType)
                    {
                        case EMapTextureExportType.Height or EMapTextureExportType.Normal
                            when landscapeComponent.TryGetValue(out UTexture2D heightTexture, "HeightmapTexture"):
                        {
                            heightTileInfos.Add(new MapTextureTileInfo(heightTexture.DecodeImageSharp()!, x, y));
                            break;
                        }
                        case EMapTextureExportType.Weight 
                        when landscapeComponent.TryGetValue(out UTexture2D[] weightmapTextures, "WeightmapTextures")
                             && landscapeComponent.TryGetValue(out FWeightmapLayerAllocationInfo[] weightmapAllocations, "WeightmapLayerAllocations"):
                        {
                            var weightmapImages = weightmapTextures.Select(tex => tex.DecodeImageSharp()).ToList();
                            foreach (var weightmapLayerInfo in weightmapAllocations)
                            {
                                var layerInfo = await weightmapLayerInfo.LayerInfo.LoadAsync();
                                var layerName = layerInfo.Get<FName>("LayerName").Text;
                                weightmapTileInfos.TryAdd(layerName, []);
                                weightmapTileInfos[layerName].Add(new MapTextureTileInfo(weightmapImages[weightmapLayerInfo.WeightmapTextureIndex]!, x, y, weightmapLayerInfo.WeightmapTextureChannel));
                            }

                            break;
                        }
                    }
                }
            }

            return proxyDetectedCount;
        }

        var proxyCount = await CollectTileInfos(_level);

        if (proxyCount == 0)
        {
            var worldSettings = _level.Get<UObject>("WorldSettings");
            var worldPartition = worldSettings.Get<UObject>("WorldPartition");
            var runtimeHash = worldPartition.Get<UObject>("RuntimeHash");
            foreach (var streamingGrid in runtimeHash.GetOrDefault("StreamingGrids", Array.Empty<FStructFallback>()))
            {
                foreach (var gridLevel in streamingGrid.GetOrDefault("GridLevels", Array.Empty<FStructFallback>()))
                foreach (var layerCell in gridLevel.GetOrDefault("LayerCells", Array.Empty<FStructFallback>()))
                foreach (var gridCell in layerCell.GetOrDefault("GridCells", Array.Empty<UObject>()))
                {
                    var levelStreaming = gridCell.GetOrDefault<UObject?>("LevelStreaming");
                    if (levelStreaming is null) continue;

                    var worldAsset = levelStreaming.Get<FSoftObjectPath>("WorldAsset");
                    var world = await worldAsset.LoadAsync<UWorld>();
                    var level = await world.PersistentLevel.LoadAsync<ULevel>();
                    proxyCount += await CollectTileInfos(level);
                }

                break;
            }
        }

        switch (exportType)
        {
            // TODO can definitely be done using index offset for faster memory access
            case EMapTextureExportType.Height:
            {
                async Task ExportHeightMap(string folderName = "")
                {
                    var heightImage = new Image<L16>(2048, 2048);
                    heightImage.Mutate(ctx => ctx.Fill(Info.Name.Equals("Rufus") ? Color.Black : HeightBaseColor));
                    foreach (var heightTileInfo in heightTileInfos)
                    {
                        if (heightTileInfo.Image.Width > 128 || heightTileInfo.Image.Height > 128) continue;
                        PixelOperations(heightTileInfo, (color, x, y, _) =>
                        {
                            var correctedColor = (ushort) ((color.R << 8) | color.G);
                            heightImage[x, y] = new L16(correctedColor);
                        });
                    }

                    await heightImage.SaveAsPngAsync(GetExportPath($"Height_{WorldName}", folderName));
                }
                
                if (Info.Name.Equals("Rufus"))
                {
                    await ExportHeightMap("BaseMap");
                    await ExportHeightMap("SnowBiome");
                }
                else
                {
                    await ExportHeightMap();
                }
                break;
            }
            case EMapTextureExportType.Normal:
            {
                async Task ExportNormalMap(string folderName = "", Predicate<MapTextureTileInfo>? predicate = null)
                {
                    var normalImage = new Image<Rgb24>(2048, 2048);
                    normalImage.Mutate(ctx => ctx.Fill(NormalBaseColor));
                    foreach (var heightTileInfo in heightTileInfos)
                    {
                        if (predicate is not null && !predicate(heightTileInfo)) continue;
                        PixelOperations(heightTileInfo, (color, x, y, _) =>
                        {
                            normalImage[x, y] = new Rgb24(color.B, color.A, 0xFF);
                        });
                    }

                    await normalImage.SaveAsPngAsync(GetExportPath($"Normal_{WorldName}", folderName));
                }
                
                if (Info.Name.Equals("Rufus"))
                {
                    await ExportNormalMap("BaseMap", info => info.Image.Width <= 128 || info.Image.Height <= 128);
                    await ExportNormalMap("SnowBiome", info => info.Image.Width > 128 || info.Image.Height > 128);
                }
                else
                {
                    await ExportNormalMap();
                }
               
                break;
            }
            case EMapTextureExportType.Weight:
            {
                async Task ExportWeightMap(string layerName, List<MapTextureTileInfo> weightTileInfos, string folderName = "", Predicate<MapTextureTileInfo>? predicate = null)
                {
                    var weightImage = new Image<L8>(2048, 2048);
                    foreach (var weightTileInfo in weightTileInfos)
                    {
                        if (predicate is not null && !predicate(weightTileInfo)) continue;
                        PixelOperations(weightTileInfo, (color, x, y, channel) =>
                        {
                            var l8 = channel switch
                            {
                                0 => color.R,
                                1 => color.G,
                                2 => color.B,
                                3 => color.A
                            };

                            weightImage[x, y] = new L8(l8);
                        });
                    }
                    
                    await weightImage.SaveAsPngAsync(GetExportPath($"Weight_{layerName}_{WorldName}", folderName));
                }
                
                foreach (var (layerName, weightTileInfos) in weightmapTileInfos)
                {
                    if (Info.Name.Equals("Rufus"))
                    {
                        await ExportWeightMap(layerName, weightTileInfos, "BaseMap", info => info.Image.Width <= 128 || info.Image.Height <= 128);
                        await ExportWeightMap(layerName, weightTileInfos, "SnowBiome", info => info.Image.Width > 128 || info.Image.Height > 128);
                    }
                    else
                    {
                        await ExportWeightMap(layerName, weightTileInfos);
                    }
                }
                break;
            }
        }
        
        Launch(ExportPath);
    }

    private string GetExportPath(string name, string folderName = "")
    {
        if (!string.IsNullOrEmpty(folderName))
        {
            var folderPath = Path.Combine(ExportPath, folderName);
            Directory.CreateDirectory(folderPath);
            return Path.Combine(folderPath, name + ".png");

        }
        return Path.Combine(ExportPath, name + ".png");
    }
    
    private void PixelOperations(MapTextureTileInfo tileInfo, Action<Rgba32, int, int, int> action)
    {
        for (var texX = 0; texX < tileInfo.Image.Width; texX++)
        {
            for (var texY = 0; texY < tileInfo.Image.Height; texY++)
            {
                var color = tileInfo.Image[texX, texY];
                var xOffset = texX + tileInfo.X;
                var yOffset = texY + tileInfo.Y;

                action(color, xOffset, yOffset, tileInfo.ChannelIndex);
            }
        }
    }

    public override string ToString()
    {
        return $"{Info.SourceName}: {Info.Name}";
    }
}

public partial class WorldPartitionGrid : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ToolTipNames))] private List<WorldPartitionGridMap> _maps = [];
    public string ToolTipNames => string.Join("\n", Maps.Select(map => map.Name));
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OffsetMargin))] private FVector _position;
    [ObservableProperty] private FVector _originalPosition;
    public Thickness OffsetMargin => new(Position.X * MapInfo.Scale + MapInfo.XOffset, Position.Y * MapInfo.Scale + MapInfo.YOffset, 0, 0);

    [ObservableProperty] private int _cellSize;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(GridBrush))] private bool _isSelected;

    public SolidColorBrush GridBrush => IsSelected ? SolidColorBrush.Parse("#00CFFF"): SolidColorBrush.Parse("#808080");
    

    public MapInfo MapInfo;

    public WorldPartitionGrid(FVector position, MapInfo mapInfo)
    {
        OriginalPosition = position;

        var rotatedPosition = RotateAboutOrigin(new Vector2(position.X, position.Y), Vector2.Zero);
        Position = new FVector(rotatedPosition.X, rotatedPosition.Y, 0);
        MapInfo = mapInfo;
        CellSize = mapInfo.CellSize;
    }
    
    public Vector2 RotateAboutOrigin(Vector2 point, Vector2 origin)
    {
        return Vector2.Transform(point - origin, System.Numerics.Matrix3x2.CreateRotation(-MathF.PI/2f)) + origin;
    } 
    
    
}

public partial class WorldPartitionGridMap : ObservableObject
{
    [ObservableProperty] private string _path;
    public string Name => Path.SubstringBeforeLast(".").SubstringAfterLast("/");

    public WorldPartitionGridMap(string path)
    {
        Path = path;
    }
    
    
    [RelayCommand]
    public async Task CopyID()
    {
        await Clipboard.SetTextAsync(Name);
    }
}

public record MapTextureTileInfo(Image<Rgba32> Image, int X, int Y, int ChannelIndex = -1);

public enum EMapTextureExportType
{
    Height,
    Normal,
    Weight
}

public record MapInfo(string Name, string MapPath, string MinimapPath, string MaskPath, float Scale, int XOffset, int YOffset, int CellSize, int MinGridDistance, bool UseMask, bool IsNonDisplay = false, string SourceName = "Battle Royale")
{
    public static MapInfo CreateNonDisplay(string name, string mapPath)
    {
        return new MapInfo(name, mapPath, null, null, 0, 0, 0, 0, 12800, false, true);
    }
    
    public bool IsValid()
    {
        return CUE4ParseVM.Provider.Files.ContainsKey(CUE4ParseVM.Provider.FixPath(MapPath + ".umap"));
    }
}