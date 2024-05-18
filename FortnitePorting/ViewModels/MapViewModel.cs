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
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.ViewModels;

public partial class MapViewModel : ViewModelBase
{
    [ObservableProperty] private WriteableBitmap _mapBitmap;
    [ObservableProperty] private WriteableBitmap _maskBitmap;
    
    [ObservableProperty] private ObservableCollection<string> _gridNames = [];
    [ObservableProperty] private string _selectedGrid;
    
    [ObservableProperty] private ObservableCollection<Grid> _grids = [];

    private const string MAP_PATH = "FortniteGame/Content/Athena/Helios/Maps/Helios_Terrain";
    private const string MINIMAP_PATH = "FortniteGame/Content/Athena/Apollo/Maps/UI/Apollo_Terrain_Minimap";
    private const string MASK_PATH = "FortniteGame/Content/Athena/Apollo/Maps/UI/T_MiniMap_Mask";

    public override async Task Initialize()
    {
       /*var mapTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(MINIMAP_PATH);
        var mapBitmap = mapTexture.Decode();
        await File.WriteAllBytesAsync("D:/map.png", mapBitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray());

        var drawBitmap = new SKBitmap(new SKImageInfo(mapBitmap.Width, mapBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using var canvas = new SKCanvas(drawBitmap);
        canvas.RotateDegrees(90, drawBitmap.Width/2, drawBitmap.Height/2);
        canvas.DrawBitmap(mapBitmap, 0, 0);
        canvas.RotateDegrees(-90, drawBitmap.Width/2, drawBitmap.Height/2);
        
        var world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(MAP_PATH);
        var level = world.PersistentLevel.Load<ULevel>();
        if (level is null) return;

        var alreadyTexts = new Dictionary<FVector, int>();
        var datas = new List<UObject>();
        var worldSettings = level.Get<UObject>("WorldSettings");
        var worldPartition = worldSettings.Get<UObject>("WorldPartition");
        var runtimeHash = worldPartition.Get<UObject>("RuntimeHash");
        foreach (var streamingGrid in runtimeHash.GetOrDefault("StreamingGrids", Array.Empty<FStructFallback>()))
        {
            var gridName = streamingGrid.Get<FName>("GridName").Text;
            GridNames.Add(gridName);
            
            foreach (var gridLevel in streamingGrid.GetOrDefault("GridLevels", Array.Empty<FStructFallback>()))
            foreach (var layerCell in gridLevel.GetOrDefault("LayerCells", Array.Empty<FStructFallback>()))
            foreach (var gridCell in layerCell.GetOrDefault("GridCells", Array.Empty<UObject>()))
            {
                if (gridCell.GetOrDefault<bool>("bIsHLOD")) continue;
                
                var levelStreaming = gridCell.GetOrDefault<UObject?>("LevelStreaming");
                if (levelStreaming is null) continue;

                var worldAsset = levelStreaming.Get<FSoftObjectPath>("WorldAsset");
                var worldName = worldAsset.AssetPathName.Text.SubstringBeforeLast(".").SubstringAfterLast("/");

                var subWorld = worldAsset.Load<UWorld>();
                //var subLevel = subWorld.PersistentLevel.Load<ULevel>();

                var runtimeCellData = gridCell.Get<UObject>("RuntimeCellData");
                datas.Add(runtimeCellData);
                var pos = runtimeCellData.GetOrDefault<FVector>("Position");
                alreadyTexts.TryAdd(pos, 0);
                
                var factor = 0.008f;
                var offsetx = mapBitmap.Width / 2 - 64 - 32 - 2;
                var offsety = mapBitmap.Height / 2 - 64 - 40;
                var bites = new byte[3];
                Random.Shared.NextBytes(bites);

                if (alreadyTexts[pos] == 0)
                {
                    var col = new SKColor(bites[0], bites[1], bites[2]);
                    canvas.DrawRect(pos.X * factor + offsetx, pos.Y * factor + offsety, 96, 96, new SKPaint
                    {
                        Color = col.WithAlpha(255),
                    });
                }
                
                canvas.DrawText(worldName[..4], pos.X * factor + offsetx + 96f/2, pos.Y * factor + offsety + 96f/4 + 96 * 0.25f* alreadyTexts[pos], new SKFont(SKTypeface.Default, 28), new SKPaint
                {
                    Color = SKColors.Black,
                    TextAlign = SKTextAlign.Center
                });
                
                alreadyTexts[pos]++;
            }
            
            break;
        }
        
        await File.WriteAllTextAsync("D:/map.json", JsonConvert.SerializeObject(datas));

        MapBitmap = drawBitmap.ToWriteableBitmap();
        MapBitmap.Save("D:/pos.png");*/
       
        var mapTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(MINIMAP_PATH);
        MapBitmap = mapTexture.Decode()!.ToWriteableBitmap();

        var maskTexture = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(MASK_PATH);
        MaskBitmap = maskTexture.Decode()!.ToWriteableBitmap();

        var world = await CUE4ParseVM.Provider.LoadObjectAsync<UWorld>(MAP_PATH);
        var level = world.PersistentLevel.Load<ULevel>();
        if (level is null) return;

        SolidColorBrush brush = null;
        await TaskService.RunDispatcherAsync(() =>
        {
            brush = new SolidColorBrush(Colors.White);
        });

        var exisitingGrids = new List<FVector>();
        var worldSettings = level.Get<UObject>("WorldSettings");
        var worldPartition = worldSettings.Get<UObject>("WorldPartition");
        var runtimeHash = worldPartition.Get<UObject>("RuntimeHash");
        foreach (var streamingGrid in runtimeHash.GetOrDefault("StreamingGrids", Array.Empty<FStructFallback>()))
        {
            var gridName = streamingGrid.Get<FName>("GridName").Text;
            GridNames.Add(gridName);

            foreach (var gridLevel in streamingGrid.GetOrDefault("GridLevels", Array.Empty<FStructFallback>()))
            foreach (var layerCell in gridLevel.GetOrDefault("LayerCells", Array.Empty<FStructFallback>()))
            foreach (var gridCell in layerCell.GetOrDefault("GridCells", Array.Empty<UObject>()))
            {
                var levelStreaming = gridCell.GetOrDefault<UObject?>("LevelStreaming");
                if (levelStreaming is null) continue;

                var worldAsset = levelStreaming.Get<FSoftObjectPath>("WorldAsset");

                var runtimeCellData = gridCell.Get<UObject>("RuntimeCellData");
                var pos = runtimeCellData.GetOrDefault<FVector>("Position");
                if (exisitingGrids.Contains(pos)) continue;
                exisitingGrids.Add(pos);
                
                var factor = 0.014f;
                var offsetx = -64 -64;
                var offsety = -64;
                Grids.Add(new Grid
                {
                    Name = worldAsset.AssetPathName.Text.SubstringBeforeLast(".").SubstringAfterLast("/"),
                    Path = worldAsset.AssetPathName.Text,
                    Margin = new Thickness(pos.X * factor + offsetx, pos.Y * factor + offsety, 0, 0),
                    Size = 92,
                    Color = brush
                });
            }

            break;
        }

        SelectedGrid = GridNames[0];
    }

    public void Restart()
    {
        GridNames.Clear();
        Grids.Clear();
        
        TaskService.Run(Initialize);
    }
}

public class Grid
{
    public string Name {get; set;}
    public string Path {get; set;}
    
    public int Size { get; set; }

    public Thickness Margin { get; set; }
    public SolidColorBrush Color { get; set; }
}