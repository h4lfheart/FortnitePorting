using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports;
using FortnitePorting.Views.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FortnitePorting.Tools;

public static class HeightmapExporter
{
    public const int Size = 2048 - 15;

    public static void Export()
    {
        var world = AppVM.CUE4ParseVM.Provider.LoadObject<UWorld>("FortniteGame/Content/Athena/Asteria/Maps/Asteria_Terrain.Asteria_Terrain");
        var level = world.PersistentLevel.Load<ULevel>();
        if (level is null) return;

        // Gather Textures
        var heightTextures = new List<TileData?>();
        var weightmapLayerTextures = new Dictionary<string, List<TileData?>>(); // LayerName : List of All Textures
        foreach (var actor in level.Actors)
        {
            if (!actor.Name.StartsWith("LandscapeStreamingProxy")) continue;

            var landscapeProxy = actor.Load();
            if (landscapeProxy is null) continue;

            var landscapeComponents = landscapeProxy.GetOrDefault("LandscapeComponents", Array.Empty<UObject>());
            //var landscapeNanite = landscapeProxy.Get<UObject>("NaniteComponent");
            //var landscapeMesh = landscapeNanite.Get<UStaticMesh>("StaticMesh");
            //ExportHelpers.SaveLandscapeMesh(landscapeMesh, landscapeProxy.Name);

            foreach (var component in landscapeComponents)
            {
                var x = component.GetOrDefault<int>("SectionBaseX");
                var y = component.GetOrDefault<int>("SectionBaseY");
                if (component.TryGetValue(out UTexture2D heightTexture, "HeightmapTexture"))
                {
                    var image = heightTexture.DecodeImageSharp<Bgra32>();
                    heightTextures.Add(new TileData(image, x, y));
                }

                if (component.TryGetValue(out UTexture2D[] weightmapTextures, "WeightmapTextures") && component.TryGetValue(out FWeightmapLayerAllocationInfo[] weightmapAllocations, "WeightmapLayerAllocations"))
                {
                    var weightmapImages = new Image<Bgra32>?[weightmapTextures.Length];
                    for (var i = 0; i < weightmapTextures.Length; i++)
                    {
                        weightmapImages[i] = weightmapTextures[i].DecodeImageSharp<Bgra32>();
                    }

                    foreach (var weightmapLayerInfo in weightmapAllocations)
                    {
                        var layerName = weightmapLayerInfo.LayerInfo.LayerName.Text;
                        if (!weightmapLayerTextures.ContainsKey(layerName))
                        {
                            weightmapLayerTextures[layerName] = new List<TileData?>();
                        }

                        weightmapLayerTextures[layerName].Add(new TileData(weightmapImages[weightmapLayerInfo.WeightmapTextureIndex], x, y, weightmapLayerInfo.WeightmapTextureChannel));
                    }
                }
            }
        }

        // Height/Normal Map
        if (AppVM.HeightmapVM.ExportHeightmap)
        {
            Log.Information("Exporting Heightmap: {Type}", "Height");

            var height = new Image<L16>(Size, Size);
            height.Mutate(x => x.Fill(Color.FromRgb(0x79, 0x79, 0x97)));
            IteratePixels(heightTextures, (color, x, y, _) =>
            {
                var corrected = (ushort)((color.R << 8) | color.G);
                height[x, y] = new L16(corrected);
            });
            height.SaveAsPng(Path.Combine(App.MapFolder.FullName, $"{world.Name}_Height.png"));
            SetPreviewImage(height);
        }

        if (AppVM.HeightmapVM.ExportNormalmap)
        {
            Log.Information("Exporting Normalmap: {Type}", "Normal");

            var normal = new Image<Rgb24>(Size, Size);
            normal.Mutate(x => x.Fill(Color.FromRgb(0x7f, 0x7f, 0xFF)));
            IteratePixels(heightTextures, (color, x, y, _) =>
            {
                normal[x, y] = new Rgb24(color.B, color.A, 255);
            });
            normal.SaveAsPng(Path.Combine(App.MapFolder.FullName, $"{world.Name}_Normal.png"));
            SetPreviewImage(normal);
        }

        // Weightmaps
        if (AppVM.HeightmapVM.ExportWeightmap)
        {
            foreach (var (layerName, weightmapTextures) in weightmapLayerTextures)
            {
                Log.Information("Exporting Weightmap: {LayerName}", layerName);
                var map = new Image<L8>(Size, Size);
                IteratePixels(weightmapTextures, (color, x, y, channel) =>
                {
                    var l8 = channel switch
                    {
                        0 => color.R,
                        1 => color.G,
                        2 => color.B,
                        3 => color.A
                    };

                    map[x, y] = new L8(l8);
                });
                map.SaveAsPng(Path.Combine(App.MapFolder.FullName, $"{world.Name}_{layerName}.png"));
                SetPreviewImage(map);
            }
        }

        AppHelper.Launch(App.MapFolder.FullName);
    }

    public static void IteratePixels(List<TileData?> textures, Action<Bgra32, int, int, int> action)
    {
        foreach (var textureData in textures)
        {
            if (textureData is null) return;

            var (heightTexture, x, y, channelIndex) = textureData;
            if (heightTexture is null) continue;

            for (var texX = 0; texX < heightTexture.Width; texX++)
            {
                for (var texY = 0; texY < heightTexture.Height; texY++)
                {
                    var color = heightTexture[texX, texY];
                    var xOffset = texX + x;
                    var yOffset = texY + y;

                    action(color, xOffset, yOffset, channelIndex);
                }
            }
        }
    }

    public record TileData(Image<Bgra32>? Image, int X, int Y, int channelIndex = -1);

    public static void SetPreviewImage(Image image)
    {
        Application.Current.Dispatcher.Invoke(() => AppVM.HeightmapVM.ImageSource = image.ToBitmapImage(), DispatcherPriority.Background);
    }
}