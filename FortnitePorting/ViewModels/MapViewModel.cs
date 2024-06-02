using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Models;
using FortnitePorting.Models.Unreal;
using FortnitePorting.OpenGL;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Windows;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;

namespace FortnitePorting.ViewModels;

public partial class MapViewModel : ViewModelBase
{
    [ObservableProperty] private WriteableBitmap _mapBitmap;
    [ObservableProperty] private WriteableBitmap _maskBitmap;
    [ObservableProperty] private string _worldName = string.Empty;
    [ObservableProperty] private bool _dataLoaded = false;
    
    [ObservableProperty] private ObservableCollection<WorldPartitionGrid> _grids = [];
    public int GridCount => Grids.Sum(grid => grid.Maps.Count);

    private ULevel Level;

    // todo create presets for diff versions?
    private const string MAP_PATH = "FortniteGame/Content/Athena/Helios/Maps/Helios_Terrain";
    private const string MINIMAP_PATH = "FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap";
    private const string MASK_PATH = "FortniteGame/Content/Athena/Apollo/Maps/UI/T_MiniMap_Mask";

    private string ExportPath => Path.Combine(MapsFolder.FullName, WorldName);

    private static Color HeightBaseColor = Color.FromRgb(0x79, 0x79, 0x79);
    private static Color NormalBasecolor = Color.FromRgb(0x7f, 0x7f, 0xFF);

    public MapViewModel()
    {
        // todo turn this into attribute if possible -> NotifyCollectionChangedFor
        Grids.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(GridCount));
    }

    public override async Task Initialize()
    {
        WorldName = MAP_PATH.SubstringAfterLast("/");
        
        var mapTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(MINIMAP_PATH);
        MapBitmap = mapTexture.Decode()!.ToWriteableBitmap();

        var maskTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(MASK_PATH);
        MaskBitmap = maskTexture.Decode()!.ToWriteableBitmap();

        var world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(MAP_PATH);
        
        Level = world.PersistentLevel.Load<ULevel>()!;
        if (Level is null) return;

        var worldSettings = Level.Get<UObject>("WorldSettings");
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

                var runtimeCellData = gridCell.Get<UObject>("RuntimeCellData");
                var position = runtimeCellData.GetOrDefault<FVector>("Position");
                if (Grids.FirstOrDefault(grid => grid.Position == position) is { } targetGrid)
                {
                    targetGrid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                }
                else
                {
                    var grid = new WorldPartitionGrid(position);
                    grid.Maps.Add(new WorldPartitionGridMap(worldAsset.AssetPathName.Text));
                    Grids.Add(grid);
                }
            }

            break;
        }

        Directory.CreateDirectory(ExportPath);
        DataLoaded = true;
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

    private async Task ExportLandscapeMaps(EMapTextureExportType exportType)
    {
        var heightTileInfos = new List<MapTextureTileInfo>();
        var weightmapTileInfos = new Dictionary<string, List<MapTextureTileInfo>>();
        
        foreach (var actor in Level.Actors)
        {
            if (!actor.Name.StartsWith("LandscapeStreamingProxy")) continue;

            var landscapeStreamingProxy = await actor.LoadAsync();
            if (landscapeStreamingProxy is null) continue;
            
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

        switch (exportType)
        {
            case EMapTextureExportType.Height:
            {
                // TODO can definitely be done using index offset for faster memory access
                var heightImage = new Image<L16>(2048, 2048);
                heightImage.Mutate(ctx => ctx.Fill(HeightBaseColor));
                foreach (var heightTileInfo in heightTileInfos)
                {
                    PixelOperations(heightTileInfo, (color, x, y, _) =>
                    {
                        var correctedColor = (ushort) ((color.R << 8) | color.G);
                        heightImage[x, y] = new L16(correctedColor);
                    });
                }
                await heightImage.SaveAsPngAsync(GetExportPath($"Height_{WorldName}"));
                break;
            }
            case EMapTextureExportType.Normal:
            {
                var normalImage = new Image<Rgb24>(2048, 2048);
                normalImage.Mutate(ctx => ctx.Fill(NormalBasecolor));
                foreach (var heightTileInfo in heightTileInfos)
                {
                    PixelOperations(heightTileInfo, (color, x, y, _) =>
                    {
                        normalImage[x, y] = new Rgb24(color.B, color.A, 0xFF);
                    });
                }

                await normalImage.SaveAsPngAsync(GetExportPath($"Normal_{WorldName}"));
                break;
            }
            case EMapTextureExportType.Weight:
            {
                foreach (var (layerName, weightTileInfos) in weightmapTileInfos)
                {
                    var weight = new Image<L8>(2048, 2048);
                    foreach (var weightTileInfo in weightTileInfos)
                    {
                        PixelOperations(weightTileInfo, (color, x, y, channel) =>
                        {
                            var l8 = channel switch
                            {
                                0 => color.R,
                                1 => color.G,
                                2 => color.B,
                                3 => color.A
                            };

                            weight[x, y] = new L8(l8);
                        });
                    }
                    
                    await weight.SaveAsPngAsync(GetExportPath($"Weight_{layerName}_{WorldName}"));
                }
                break;
            }
        }
        
        Launch(ExportPath);
    }

    private string GetExportPath(string name)
    {
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
}

public partial class WorldPartitionGrid : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ToolTipNames))] private List<WorldPartitionGridMap> _maps = [];
    public string ToolTipNames => string.Join("\n", Maps.Select(map => map.Name));
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(OffsetMargin))] private FVector _position;
    public Thickness OffsetMargin => new(Position.X * SCALE_FACTOR + X_OFFSET, Position.Y * SCALE_FACTOR + Y_OFFSET, 0, 0);
    
    private const float SCALE_FACTOR = 0.014f;
    private const int X_OFFSET = -128;
    private const int Y_OFFSET = 0;

    public WorldPartitionGrid(FVector position)
    {
        Position = position;
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
    public async Task Preview()
    {
        var world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(Path);
        var level = await world.PersistentLevel.LoadAsync<ULevel>();
        ModelPreviewWindow.Preview(Name, level);
    }
    
    [RelayCommand]
    public async Task Export()
    {
        AppVM.Message(string.Empty, "Exporting world partition grids has not been implemented yet.");
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