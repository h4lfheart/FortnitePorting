using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.ViewModels;

public partial class MapViewModel : ViewModelBase
{
    [ObservableProperty] private WriteableBitmap _mapBitmap;
    [ObservableProperty] private WriteableBitmap _maskBitmap;
    [ObservableProperty] private string _worldName = string.Empty;
    [ObservableProperty] private bool _dataLoaded = false;
    
    [ObservableProperty] private ObservableCollection<WorldPartitionGrid> _grids = [];

    // todo create presets for diff versions?
    private const string MAP_PATH = "FortniteGame/Content/Athena/Helios/Maps/Helios_Terrain";
    private const string MINIMAP_PATH = "FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap";
    private const string MASK_PATH = "FortniteGame/Content/Athena/Apollo/Maps/UI/T_MiniMap_Mask";

    private string ExportPath => Path.Combine(MapsFolder.FullName, WorldName);

    public override async Task Initialize()
    {
        var mapTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(MINIMAP_PATH);
        MapBitmap = mapTexture.Decode()!.ToWriteableBitmap();

        var maskTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(MASK_PATH);
        MaskBitmap = maskTexture.Decode()!.ToWriteableBitmap();

        var world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(MAP_PATH);
        WorldName = world.Name;
        
        var level = world.PersistentLevel.Load<ULevel>();
        if (level is null) return;

        var worldSettings = level.Get<UObject>("WorldSettings");
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
    }
    
    [RelayCommand]
    public async Task ExportHeightmap()
    {
        //MapBitmap.Save(GetExportPath($"Heightmap_{WorldName}"));
    }

    private string GetExportPath(string name)
    {
        return Path.Combine(ExportPath, name + ".png");
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
    private const int Y_OFFSET = -64;

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
        ModelPreviewWindow.Preview(level);
    }

}