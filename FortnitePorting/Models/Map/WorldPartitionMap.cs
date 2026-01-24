using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Shared.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageExtensions = FortnitePorting.Extensions.ImageExtensions;

namespace FortnitePorting.Models.Map;


public partial class WorldPartitionMap : ObservableObject
{
    [ObservableProperty] private MapInfo _mapInfo;
    [ObservableProperty] private Bitmap? _mapBitmap;
    [ObservableProperty] private Bitmap _maskBitmap;
    [ObservableProperty] private string _worldName = string.Empty;
    [ObservableProperty] private bool _dataLoaded = false;
    [ObservableProperty] private EMapTextureExportType _textureExportType = EMapTextureExportType.Minimap;
    
    [ObservableProperty] private bool _worldFlagsActors = true;
    [ObservableProperty] private bool _worldFlagsInstancedFoliage = true;
    [ObservableProperty] private bool _worldFlagsLandscape = true;
    [ObservableProperty] private bool _worldFlagsHLODs = false;

    [ObservableProperty] private bool _includeMainLevel;
    
    [ObservableProperty] private ObservableCollection<WorldPartitionGridMap> _selectedMaps = [];
    
    [ObservableProperty] private ObservableCollection<WorldPartitionGrid> _grids = [];
    
    public DirectoryInfo MapsFolder => new(Path.Combine(App.ApplicationDataFolder.FullName, "Maps"));
    private string ExportPath => Path.Combine(MapsFolder.FullName, WorldName);
    
    private UWorld _world;
    private ULevel _level;
    
    private static readonly Color HeightBaseColor = Color.FromRgb(0x79, 0x79, 0x79);
    private static readonly Color NormalBaseColor = Color.FromRgb(0x7f, 0x7f, 0xFF);

    public WorldPartitionMap(MapInfo info)
    {
        MapInfo = info;
    }

    public async Task Load()
    {
        Grids = [];
        
        if (!MapInfo.IsNonDisplay)
        {
            var maskTexture = await UEParse.Provider.SafeLoadPackageObjectAsync<UTexture2D>(MapInfo.UseMask ? MapInfo.MaskPath : "FortniteGame/Content/Global/Textures/Default/Blanks/T_White");
            MaskBitmap = maskTexture!.Decode()!.ToSkBitmap().ToOpacityMask().ToWriteableBitmap();
        
            var mapTexture = await UEParse.Provider.SafeLoadPackageObjectAsync<UTexture2D>(MapInfo.MinimapPath);
            MapBitmap = mapTexture.Decode()!.ToWriteableBitmap();
        }
        
        WorldName = MapInfo.MapPath.SubstringAfterLast("/");
        
        _world = await UEParse.Provider.SafeLoadPackageObjectAsync<UWorld>(MapInfo.MapPath);
        _level = await _world.PersistentLevel.LoadAsync<ULevel>();

        async Task RuntimeHash(UObject runtimeHash)
        {
            var cellToStreamingData = runtimeHash.GetOrDefault<UScriptMap?>("CellToStreamingData");
            foreach (var streamingData in runtimeHash.GetOrDefault("RuntimeStreamingData", Array.Empty<FStructFallback>()))
            {
                var cells = new List<FPackageIndex>();
                cells.AddRange(streamingData.GetOrDefault("SpatiallyLoadedCells", Array.Empty<FPackageIndex>()));
                cells.AddRange(streamingData.GetOrDefault("NonSpatiallyLoadedCells", Array.Empty<FPackageIndex>()));

                foreach (var cell in cells)
                {
                    var gridCell = await cell.LoadAsync();
                    if (gridCell is null) continue;
                    
                    var position = FVector.ZeroVector;
                    var runtimeCellData = gridCell.Get<UObject>("RuntimeCellData");
                    var boundsProperty = runtimeCellData.GetOrDefault<StructProperty?>("CellBounds");
                    if (boundsProperty?.GetValue<FBox>() is { } bounds)
                    {
                        position = bounds.GetCenter();

                        // please don't break other maps
                        position.X -= position.X % MapInfo.MinGridDistance;
                        position.Y -= position.Y % MapInfo.MinGridDistance;
                    }

                    FSoftObjectPath? worldAssetPath = null;
                    if (gridCell.GetOrDefault<UObject?>("LevelStreaming") is { } levelStreamingObject)
                    {
                        worldAssetPath = levelStreamingObject.Get<FSoftObjectPath>("WorldAsset");
                    }

                    if (worldAssetPath is null && cellToStreamingData is not null)
                    {
                        var (worldNameProperty, worldDataProperty) = cellToStreamingData.Properties.FirstOrDefault(prop =>
                            prop.Key.GetValue<FName?>()?.Text?.Equals(gridCell.Name) ?? false);

                        var worldData = worldDataProperty?.GetValue<FStructFallback>();
                        worldAssetPath = worldData?.GetOrDefault<FSoftObjectPath>("WorldAsset");
                    }
                    
                    if (worldAssetPath is not { } worldAsset) continue;
                
                    if (Grids.FirstOrDefault(grid => grid.OriginalPosition == position) is { } targetGrid)
                    {
                        targetGrid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                    }
                    else
                    {
                        var grid = new WorldPartitionGrid(position, MapInfo);
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
                        var grid = new WorldPartitionGrid(position, MapInfo);
                        grid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                        Grids.Add(grid);
                    }
                }
            }
        }


        if (_level.GetOrDefault<UObject>("WorldSettings") is { } worldSettings
            && worldSettings.GetOrDefault<UObject>("WorldPartition") is { } worldPartition
            && worldPartition.GetOrDefault<UObject>("RuntimeHash") is { } worldPartitionRuntimeHash)
        {
            await RuntimeHash(worldPartitionRuntimeHash);
        }

        var streamingObjectFiles = UEParse.Provider.Files.Where(kvp =>
            kvp.Key.Contains($"{_world.Name}/_Generated_/StreamingObject", StringComparison.OrdinalIgnoreCase));
        foreach (var (path, streamingObjectFile) in streamingObjectFiles)
        {
            var streamingObjectPackage = await UEParse.Provider.LoadPackageAsync(streamingObjectFile);
            var streamingObjectRuntimeHash = streamingObjectPackage.GetExportOrNull("RuntimeHashExternalStreamingObjectBase", StringComparison.OrdinalIgnoreCase);
            if (streamingObjectRuntimeHash is null) continue;
            
            await RuntimeHash(streamingObjectRuntimeHash);
        }
        
        Directory.CreateDirectory(ExportPath);
        DataLoaded = true;
    }

    [RelayCommand]
    public void ClearSelectedMaps()
    {
        Grids.ForEach(grid => grid.IsSelected = false);
        SelectedMaps.Clear();
        IncludeMainLevel = false;
    }
    
    [RelayCommand]
    public void SelectAllMaps()
    {
        SelectedMaps.Clear();
        Grids.ForEach(grid => grid.IsSelected = true);
        SelectedMaps.AddRange(Grids.SelectMany(x => x.Maps));
    }
    
    [RelayCommand]
    public async Task SetTextureExportType(EMapTextureExportType type)
    {
        TextureExportType = type;
    }


    [RelayCommand]
    public async Task ExportImage()
    {
        if (TextureExportType == EMapTextureExportType.Minimap)
        {
            MapBitmap.Save(GetExportPath($"Minimap_{WorldName}"));
            MaskBitmap.Save(GetExportPath($"Minimap_Mask_{WorldName}"));
            App.Launch(ExportPath);
        }
        else
        {
            await ExportLandscapeMaps(TextureExportType);
        }
    }
    
    [RelayCommand]
    public async Task Export()
    {
        var meta = AppSettings.ExportSettings.CreateExportMeta(MapVM.ExportLocation);
        meta.WorldFlags = 0;
        if (WorldFlagsActors) meta.WorldFlags |= EWorldFlags.Actors;
        if (WorldFlagsInstancedFoliage) meta.WorldFlags |= EWorldFlags.InstancedFoliage;
        if (WorldFlagsLandscape) meta.WorldFlags |= EWorldFlags.Landscape;
        if (WorldFlagsHLODs) meta.WorldFlags |= EWorldFlags.HLODs;

        SelectedMaps.ForEach(map => map.Status = EWorldPartitionGridMapStatus.Waiting);
        
        var exportedProperly = true;
        foreach (var map in SelectedMaps.ToArray())
        {
            map.Status = EWorldPartitionGridMapStatus.Exporting;
            
            var world = await UEParse.Provider.SafeLoadPackageObjectAsync<UWorld>(map.Path);
            if (world is null) continue;
            
            exportedProperly &= await Exporter.Export(world, EExportType.World, meta);
            
            map.Status = EWorldPartitionGridMapStatus.Finished;
        }
        
        if (exportedProperly && SupaBase.IsLoggedIn)
        {
            await SupaBase.PostExports(
                SelectedMaps
                    .Select(map => map.Path)
            );
        }
        
        SelectedMaps.ForEach(map => map.Status = EWorldPartitionGridMapStatus.None);
        if (exportedProperly)
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
            foreach (var actorLazy in level.Actors)
            {
                if (actorLazy.IsNull) continue;
                
                var actor = await actorLazy.LoadAsync();
                if (actor is null) continue;
                
                if (actor is not ALandscapeProxy && actor.ExportType != "Landscape") continue;

                proxyDetectedCount++;
                
                var landscapeComponents = actor.GetOrDefault("LandscapeComponents", Array.Empty<UObject>());
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
                    heightImage.Mutate(ctx => ctx.Fill(MapInfo.Id.Equals("Rufus") ? Color.Black : HeightBaseColor));

                    var minX = Math.Abs(heightTileInfos.Min(x => x.X));
                    var minY = Math.Abs(heightTileInfos.Min(x => x.Y));

                    var normalizedTiles = heightTileInfos.Select(info => info with { X = info.X + minX, Y = info.Y + minY });
                    foreach (var heightTileInfo in normalizedTiles)
                    {
                        if (heightTileInfo.Image.Width > 128 || heightTileInfo.Image.Height > 128) continue;
                        PixelOperations(heightTileInfo, (color, x, y, _) =>
                        {
                            if (x >= 2048 || y >= 2048) return;
                            
                            var correctedColor = (ushort) ((color.R << 8) | color.G);
                            heightImage[x, y] = new L16(correctedColor);
                        });
                    }

                    await heightImage.SaveAsPngAsync(GetExportPath($"Height_{WorldName}", folderName));
                }
                
                if (MapInfo.Id.Equals("Rufus"))
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
                    
                    var minX = Math.Abs(heightTileInfos.Min(x => x.X));
                    var minY = Math.Abs(heightTileInfos.Min(x => x.Y));

                    var normalizedTiles = heightTileInfos.Select(info => info with { X = info.X + minX, Y = info.Y + minY });
                    foreach (var heightTileInfo in normalizedTiles)
                    {
                        if (predicate is not null && !predicate(heightTileInfo)) continue;
                        PixelOperations(heightTileInfo, (color, x, y, _) =>
                        {
                            if (x >= 2048 || y >= 2048) return;
                            
                            normalImage[x, y] = new Rgb24(color.B, color.A, 0xFF);
                        });
                    }

                    await normalImage.SaveAsPngAsync(GetExportPath($"Normal_{WorldName}", folderName));
                }
                
                if (MapInfo.Id.Equals("Rufus"))
                {
                    await ExportNormalMap("BaseMap", info => info.Image.Width <= 128 || info.Image.Height <= 128);
                    await ExportNormalMap("SnowBiome", info => info.Image.Width > 128 || info.Image.Height > 128);
                }
                else
                {
                    await ExportNormalMap(predicate: info => info.Image is { Width: <= 128, Height: <= 128 });;
                }
               
                break;
            }
            case EMapTextureExportType.Weight:
            {
                async Task ExportWeightMap(string layerName, List<MapTextureTileInfo> weightTileInfos, string folderName = "", Predicate<MapTextureTileInfo>? predicate = null)
                {
                    var weightImage = new Image<L8>(2048, 2048);
                    
                    var minX = Math.Abs(weightTileInfos.Min(x => x.X));
                    var minY = Math.Abs(weightTileInfos.Min(x => x.Y));

                    var normalizedTiles = weightTileInfos.Select(info => info with { X = info.X + minX, Y = info.Y + minY });
                    foreach (var weightTileInfo in normalizedTiles)
                    {
                        if (predicate is not null && !predicate(weightTileInfo)) continue;
                        PixelOperations(weightTileInfo, (color, x, y, channel) =>
                        {
                            if (x >= 2048 || y >= 2048) return;
                            
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
                    if (MapInfo.Id.Equals("Rufus"))
                    {
                        await ExportWeightMap(layerName, weightTileInfos, "BaseMap", info => info.Image.Width <= 128 || info.Image.Height <= 128);
                        await ExportWeightMap(layerName, weightTileInfos, "SnowBiome", info => info.Image.Width > 128 || info.Image.Height > 128);
                    }
                    else
                    {
                        await ExportWeightMap(layerName, weightTileInfos, predicate: info => info.Image is { Width: <= 128, Height: <= 128 });
                    }
                }
                break;
            }
        }
        
        App.Launch(ExportPath);
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
        return $"{MapInfo.SourceName}: {MapInfo.Id}";
    }
}


public record MapTextureTileInfo(Image<Rgba32> Image, int X, int Y, int ChannelIndex = -1);

public enum EMapTextureExportType
{
    [Description("Minimap")]
    Minimap,
    
    [Description("Heightmap")]
    Height,
    
    [Description("Normalmap")]
    Normal,
    
    [Description("Weightmaps")]
    Weight
}