using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FortnitePorting.Exporting.Heightmaps;

public sealed class GeometryHeightmapGenerator
{
    private const float EdgeEpsilon = 1e-7f;
    private const float AreaEpsilon = 1e-12f;

    public GeometryHeightmapExportData Generate(
        IReadOnlyCollection<GeometryHeightmapMeshInstance> instances,
        string outputDirectory,
        GeometryHeightmapGenerationOptions options,
        IProgress<GeometryHeightmapProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        if (options.Resolution <= 0)
            throw new InvalidOperationException("Geometry heightmap resolution must be greater than zero.");

        if (options.Resolution > GeometryHeightmapGenerationOptions.MaxExportResolution)
            throw new InvalidOperationException($"Geometry heightmap resolution cannot exceed {GeometryHeightmapGenerationOptions.MaxExportResolution:N0}.");

        if (instances.Count == 0)
            throw new InvalidOperationException("No supported mesh geometry was found for geometry heightmap generation.");

        var sourceInstances = instances as GeometryHeightmapMeshInstance[] ?? instances.ToArray();
        var terrainInstances = sourceInstances.Where(instance => instance.IsTerrain).ToArray();
        var warnings = new List<string>();

        var useTerrainBounds = options.UseTerrainBounds && terrainInstances.Length > 0;
        var boundsInstances = useTerrainBounds ? terrainInstances : sourceInstances;
        if (options.UseTerrainBounds && terrainInstances.Length == 0)
            warnings.Add("Terrain bounds were not used because no terrain geometry was found.");

        progress?.Report(new GeometryHeightmapProgress("Measuring map bounds", 0, boundsInstances.Length));
        var bounds = MeasureBounds(boundsInstances, progress, cancellationToken);

        var rasterInstances = sourceInstances;
        var croppedToMainComponent = false;
        var clippedCount = 0;
        if (useTerrainBounds)
        {
            progress?.Report(new GeometryHeightmapProgress("Clipping geometry to terrain bounds", 0, sourceInstances.Length));
            rasterInstances = ClipInstancesToBounds(sourceInstances, bounds, progress, cancellationToken);

            clippedCount = sourceInstances.Length - rasterInstances.Length;
            if (clippedCount > 0)
                warnings.Add($"Clipped {clippedCount:N0} meshes outside the terrain bounds.");
        }

        progress?.Report(new GeometryHeightmapProgress("Rasterizing heightmap", 0, rasterInstances.Length));
        var heightmap = Rasterize(rasterInstances, bounds, options, "Rasterizing heightmap", progress, cancellationToken);

        var terrainRasterInstances = terrainInstances;
        if (options.CropToMainComponent && !options.IncludeSpawnIsland)
        {
            var componentBounds = FindPrimaryComponentBounds(heightmap, cancellationToken);
            if (componentBounds is not null &&
                !componentBounds.ApproximatelyEquals(heightmap.Bounds) &&
                componentBounds.AreaXY < heightmap.Bounds.AreaXY)
            {
                progress?.Report(new GeometryHeightmapProgress("Cropping to main island"));
                var beforeCropCount = rasterInstances.Length;
                bounds = componentBounds;
                rasterInstances = ClipInstancesToBounds(rasterInstances, bounds, null, cancellationToken);
                terrainRasterInstances = terrainInstances.Length == 0
                    ? terrainInstances
                    : ClipInstancesToBounds(terrainInstances, bounds, null, cancellationToken);

                clippedCount += beforeCropCount - rasterInstances.Length;
                croppedToMainComponent = true;
                warnings.Add("Cropped detached geometry outside the main terrain island.");

                progress?.Report(new GeometryHeightmapProgress("Rasterizing cropped heightmap", 0, rasterInstances.Length));
                heightmap = Rasterize(rasterInstances, bounds, options, "Rasterizing cropped heightmap", progress, cancellationToken);
            }
        }

        var (greyPerUnit, greyBase, rasterMinZ, rasterMaxZ) = CalculateLumaScale(heightmap.Data);

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new GeometryHeightmapProgress("Saving heightmap image"));
        SaveHeightmapPng(
            Path.Combine(outputDirectory, "heightmap.png"),
            heightmap.Data,
            heightmap.Width,
            heightmap.Height,
            greyPerUnit,
            greyBase);

        var terrainMapSaved = false;
        if (options.SaveTerrainSeparately)
        {
            if (terrainInstances.Length == 0)
            {
                warnings.Add("Terrain-only heightmap skipped because no terrain geometry was found.");
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new GeometryHeightmapProgress("Rasterizing terrain heightmap", 0, terrainRasterInstances.Length));
                var terrainHeightmap = Rasterize(terrainRasterInstances, bounds, options, "Rasterizing terrain heightmap", progress, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new GeometryHeightmapProgress("Saving terrain heightmap image"));
                SaveHeightmapPng(
                    Path.Combine(outputDirectory, "terrainmap.png"),
                    terrainHeightmap.Data,
                    terrainHeightmap.Width,
                    terrainHeightmap.Height,
                    greyPerUnit,
                    greyBase);

                terrainMapSaved = true;
            }
        }

        var exportData = new GeometryHeightmapExportData
        {
            MinX = heightmap.Bounds.MinX,
            MaxX = heightmap.Bounds.MaxX,
            MinY = heightmap.Bounds.MinY,
            MaxY = heightmap.Bounds.MaxY,
            MinZ = rasterMinZ,
            MaxZ = rasterMaxZ,
            Metre16Bit = (ushort) Math.Clamp(MathF.Round(greyPerUnit * 100.0f), 0.0f, ushort.MaxValue),
            MetrePx = heightmap.Scale * 100.0f,
            TotalMeshes = sourceInstances.Length,
            OutputWidth = heightmap.Width,
            OutputHeight = heightmap.Height,
            TerrainMapSaved = terrainMapSaved,
            UsedTerrainBounds = useTerrainBounds,
            RasterizedMeshes = rasterInstances.Length,
            TerrainMeshes = terrainInstances.Length,
            ClippedMeshes = clippedCount,
            CroppedToMainComponent = croppedToMainComponent,
            Warnings = warnings
        };

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new GeometryHeightmapProgress("Writing map metadata"));
        File.WriteAllText(
            Path.Combine(outputDirectory, "mapdata.json"),
            JsonConvert.SerializeObject(exportData, Formatting.Indented));

        return exportData;
    }

    private static GeometryHeightmapBounds MeasureBounds(
        IReadOnlyCollection<GeometryHeightmapMeshInstance> instances,
        IProgress<GeometryHeightmapProgress>? progress,
        CancellationToken cancellationToken,
        string stage = "Measuring map bounds")
    {
        var bounds = GeometryHeightmapBounds.Empty;
        var current = 0;

        foreach (var instance in instances)
        {
            cancellationToken.ThrowIfCancellationRequested();

            current++;
            foreach (var vertex in instance.Geometry.Vertices)
            {
                bounds.Include(ToHeightmapPoint(instance.Transform.TransformPosition(vertex)));
            }

            if (current % 25 == 0 || current == instances.Count)
                progress?.Report(new GeometryHeightmapProgress(stage, current, instances.Count));
        }

        if (!bounds.IsValid)
            throw new InvalidOperationException("Geometry heightmap bounds could not be measured.");

        bounds.MinZ = MathF.Floor(bounds.MinZ) - 1.0f;
        bounds.MaxZ = MathF.Floor(bounds.MaxZ) + 1.0f;
        return bounds;
    }

    private static GeometryHeightmapMeshInstance[] ClipInstancesToBounds(
        IReadOnlyCollection<GeometryHeightmapMeshInstance> instances,
        GeometryHeightmapBounds bounds,
        IProgress<GeometryHeightmapProgress>? progress,
        CancellationToken cancellationToken)
    {
        var clippedInstances = new List<GeometryHeightmapMeshInstance>(instances.Count);
        var current = 0;

        foreach (var instance in instances)
        {
            cancellationToken.ThrowIfCancellationRequested();

            current++;
            var instanceBounds = MeasureInstanceBounds(instance, cancellationToken);
            if (instanceBounds.IntersectsXY(bounds))
                clippedInstances.Add(instance);

            if (current % 100 == 0 || current == instances.Count)
                progress?.Report(new GeometryHeightmapProgress("Clipping geometry to terrain bounds", current, instances.Count));
        }

        if (clippedInstances.Count == 0)
            throw new InvalidOperationException("No supported mesh geometry was inside the terrain bounds.");

        return clippedInstances.ToArray();
    }

    private static GeometryHeightmapBounds MeasureInstanceBounds(
        GeometryHeightmapMeshInstance instance,
        CancellationToken cancellationToken)
    {
        var bounds = GeometryHeightmapBounds.Empty;
        foreach (var vertex in instance.Geometry.Vertices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bounds.Include(ToHeightmapPoint(instance.Transform.TransformPosition(vertex)));
        }

        return bounds;
    }

    private static GeometryHeightmapBounds? FindPrimaryComponentBounds(
        GeometryHeightmapRasterResult raster,
        CancellationToken cancellationToken)
    {
        var data = raster.Data;
        var width = raster.Width;
        var height = raster.Height;
        var visited = new bool[data.Length];
        var queue = new int[data.Length];
        var best = GeometryHeightmapRasterComponent.Empty;

        for (var start = 0; start < data.Length; start++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (visited[start] || !float.IsFinite(data[start]))
                continue;

            var current = FloodFillComponent(data, width, height, visited, queue, start, cancellationToken);
            if (current.Count > best.Count)
                best = current;
        }

        if (best.Count == 0 || best.Count == data.Length)
            return null;

        return RasterComponentToBounds(best, raster);
    }

    private static GeometryHeightmapRasterComponent FloodFillComponent(
        float[] data,
        int width,
        int height,
        bool[] visited,
        int[] queue,
        int start,
        CancellationToken cancellationToken)
    {
        var component = new GeometryHeightmapRasterComponent(
            Count: 0,
            MinX: width,
            MaxX: 0,
            MinY: height,
            MaxY: 0);

        var head = 0;
        var tail = 0;
        queue[tail++] = start;
        visited[start] = true;

        while (head < tail)
        {
            if ((head & 16383) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            var index = queue[head++];
            var x = index % width;
            var y = index / width;

            component = component.Include(x, y);

            TryQueue(index - 1, x > 0);
            TryQueue(index + 1, x + 1 < width);
            TryQueue(index - width, y > 0);
            TryQueue(index + width, y + 1 < height);
        }

        return component;

        void TryQueue(int index, bool canQueue)
        {
            if (!canQueue || visited[index] || !float.IsFinite(data[index]))
                return;

            visited[index] = true;
            queue[tail++] = index;
        }
    }

    private static GeometryHeightmapBounds RasterComponentToBounds(
        GeometryHeightmapRasterComponent component,
        GeometryHeightmapRasterResult raster)
    {
        var bounds = raster.Bounds;
        var xSpan = bounds.MaxX - bounds.MinX;
        var ySpan = bounds.MaxY - bounds.MinY;
        var scaleX = xSpan / Math.Max(raster.Width - 1, 1);
        var scaleY = ySpan / Math.Max(raster.Height - 1, 1);

        return new GeometryHeightmapBounds
        {
            MinX = bounds.MinX + component.MinX * scaleX,
            MaxX = bounds.MinX + component.MaxX * scaleX,
            MinY = bounds.MaxY - component.MaxY * scaleY,
            MaxY = bounds.MaxY - component.MinY * scaleY,
            MinZ = bounds.MinZ,
            MaxZ = bounds.MaxZ
        };
    }

    private static GeometryHeightmapRasterResult Rasterize(
        IReadOnlyCollection<GeometryHeightmapMeshInstance> instances,
        GeometryHeightmapBounds bounds,
        GeometryHeightmapGenerationOptions options,
        string stage,
        IProgress<GeometryHeightmapProgress>? progress,
        CancellationToken cancellationToken)
    {
        var xSpan = bounds.MaxX - bounds.MinX;
        var ySpan = bounds.MaxY - bounds.MinY;
        var zSpan = bounds.MaxZ - bounds.MinZ;
        if (xSpan == 0.0f || ySpan == 0.0f || zSpan == 0.0f)
            throw new InvalidOperationException("Geometry heightmap bounds must have non-zero extent.");

        static float ClampPercent(float value) => Math.Clamp(value, 0.0f, 100.0f) * 0.01f;

        var trimTop = ClampPercent(options.TrimSettings.Top);
        var trimRight = ClampPercent(options.TrimSettings.Right);
        var trimBottom = ClampPercent(options.TrimSettings.Bottom);
        var trimLeft = ClampPercent(options.TrimSettings.Left);

        var cropXMin = bounds.MinX + xSpan * trimLeft;
        var cropXMax = bounds.MaxX - xSpan * trimRight;
        var cropYMin = bounds.MinY + ySpan * trimTop;
        var cropYMax = bounds.MaxY - ySpan * trimBottom;

        var cropXSpan = cropXMax - cropXMin;
        var cropYSpan = cropYMax - cropYMin;
        if (cropXSpan <= 0.0f || cropYSpan <= 0.0f)
            throw new InvalidOperationException("Geometry heightmap crop bounds collapsed to zero extent.");

        var baseWidth = Math.Max(options.Resolution, 1);
        var baseHeight = Math.Max(options.Resolution, 1);

        var scaleWidth = Math.Max(baseWidth - 1, 1) / cropXSpan;
        var scaleHeight = Math.Max(baseHeight - 1, 1) / cropYSpan;
        var scale = MathF.Min(scaleWidth, scaleHeight);
        var scaleX = options.FillResolution ? scaleWidth : scale;
        var scaleY = options.FillResolution ? scaleHeight : scale;

        var outWidth = options.FillResolution || scale == scaleWidth
            ? baseWidth
            : Math.Max(1, (int) MathF.Round(cropXSpan * scale + 1.0f));
        var outHeight = options.FillResolution || scale == scaleHeight
            ? baseHeight
            : Math.Max(1, (int) MathF.Round(cropYSpan * scale + 1.0f));

        var pixelCount = checked((long) outWidth * outHeight);
        if (pixelCount > int.MaxValue)
            throw new InvalidOperationException("Requested geometry heightmap is too large.");

        var heightmap = new float[(int) pixelCount];
        Array.Fill(heightmap, float.NaN);

        var rowLocks = Enumerable.Range(0, outHeight).Select(_ => new object()).ToArray();
        var processed = 0;
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
        };

        Parallel.ForEach(instances, parallelOptions, instance =>
        {
            RasterizeInstance(instance, cropXMin, cropYMax, scaleX, scaleY, outWidth, outHeight, heightmap, rowLocks, cancellationToken);
            var done = Interlocked.Increment(ref processed);
            if (done % 25 == 0 || done == instances.Count)
                progress?.Report(new GeometryHeightmapProgress(stage, done, instances.Count));
        });

        var rasterBounds = new GeometryHeightmapBounds
        {
            MinX = cropXMin,
            MaxX = cropXMax,
            MinY = cropYMin,
            MaxY = cropYMax,
            MinZ = bounds.MinZ,
            MaxZ = bounds.MaxZ
        };

        return new GeometryHeightmapRasterResult(heightmap, outWidth, outHeight, scale, rasterBounds);
    }

    private static void RasterizeInstance(
        GeometryHeightmapMeshInstance instance,
        float cropXMin,
        float cropYMax,
        float scaleX,
        float scaleY,
        int outWidth,
        int outHeight,
        float[] heightmap,
        object[] rowLocks,
        CancellationToken cancellationToken)
    {
        var sourceVertices = instance.Geometry.Vertices;
        var screenVertices = new GeometryHeightmapPoint[sourceVertices.Length];
        for (var index = 0; index < sourceVertices.Length; index++)
        {
            var point = ToHeightmapPoint(instance.Transform.TransformPosition(sourceVertices[index]));
            screenVertices[index] = new GeometryHeightmapPoint(
                (point.X - cropXMin) * scaleX,
                (cropYMax - point.Y) * scaleY,
                point.Z);
        }

        var indices = instance.Geometry.Indices;
        for (var index = 0; index + 2 < indices.Length; index += 3)
        {
            if ((index & 1023) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            if (indices[index] > int.MaxValue ||
                indices[index + 1] > int.MaxValue ||
                indices[index + 2] > int.MaxValue)
            {
                continue;
            }

            var i0 = (int) indices[index];
            var i1 = (int) indices[index + 1];
            var i2 = (int) indices[index + 2];
            if (i0 >= screenVertices.Length || i1 >= screenVertices.Length || i2 >= screenVertices.Length)
                continue;

            var p0 = screenVertices[i0];
            var p1 = screenVertices[i1];
            var p2 = screenVertices[i2];

            var minX = (int) MathF.Max(0.0f, MathF.Floor(MathF.Min(p0.X, MathF.Min(p1.X, p2.X))));
            var maxX = (int) MathF.Min(outWidth - 1.0f, MathF.Ceiling(MathF.Max(p0.X, MathF.Max(p1.X, p2.X))));
            var minY = (int) MathF.Max(0.0f, MathF.Floor(MathF.Min(p0.Y, MathF.Min(p1.Y, p2.Y))));
            var maxY = (int) MathF.Min(outHeight - 1.0f, MathF.Ceiling(MathF.Max(p0.Y, MathF.Max(p1.Y, p2.Y))));

            if (minX > maxX || minY > maxY)
                continue;

            var area = Edge(p0.X, p0.Y, p1.X, p1.Y, p2.X, p2.Y);
            if (MathF.Abs(area) < AreaEpsilon)
                continue;

            var counterClockwise = area > 0.0f;

            for (var y = minY; y <= maxY; y++)
            {
                lock (rowLocks[y])
                {
                    var rowBase = y * outWidth;
                    for (var x = minX; x <= maxX; x++)
                    {
                        var px = x + 0.5f;
                        var py = y + 0.5f;

                        var w0 = Edge(p1.X, p1.Y, p2.X, p2.Y, px, py);
                        var w1 = Edge(p2.X, p2.Y, p0.X, p0.Y, px, py);
                        var w2 = Edge(p0.X, p0.Y, p1.X, p1.Y, px, py);

                        if (counterClockwise)
                        {
                            if (w0 < -EdgeEpsilon || w1 < -EdgeEpsilon || w2 < -EdgeEpsilon)
                                continue;
                        }
                        else if (w0 > EdgeEpsilon || w1 > EdgeEpsilon || w2 > EdgeEpsilon)
                        {
                            continue;
                        }

                        if (MathF.Abs(w0) <= EdgeEpsilon && !IsTopLeft(p1.X, p1.Y, p2.X, p2.Y))
                            continue;
                        if (MathF.Abs(w1) <= EdgeEpsilon && !IsTopLeft(p2.X, p2.Y, p0.X, p0.Y))
                            continue;
                        if (MathF.Abs(w2) <= EdgeEpsilon && !IsTopLeft(p0.X, p0.Y, p1.X, p1.Y))
                            continue;

                        var z = (w0 * p0.Z + w1 * p1.Z + w2 * p2.Z) / area;
                        var heightmapIndex = rowBase + x;
                        var current = heightmap[heightmapIndex];
                        if (float.IsNaN(current) || z > current)
                            heightmap[heightmapIndex] = z;
                    }
                }
            }
        }
    }

    private static (float GreyPerUnit, float GreyBase, float MinZ, float MaxZ) CalculateLumaScale(float[] data)
    {
        var zMin = float.PositiveInfinity;
        var zMax = float.NegativeInfinity;

        foreach (var value in data)
        {
            if (!float.IsFinite(value))
                continue;

            zMin = MathF.Min(zMin, value);
            zMax = MathF.Max(zMax, value);
        }

        if (!float.IsFinite(zMin) || !float.IsFinite(zMax))
            throw new InvalidOperationException("Rasterized geometry heightmap did not contain any finite heights.");

        var zRange = MathF.Max(zMax - zMin, 1.0f);
        var greyPerUnit = 65535.0f / zRange;
        return (greyPerUnit, -zMin * greyPerUnit, zMin, zMax);
    }

    private static void SaveHeightmapPng(
        string path,
        float[] data,
        int width,
        int height,
        float greyPerUnit,
        float greyBase)
    {
        using var image = new Image<L16>(height, width);

        for (var index = 0; index < data.Length; index++)
        {
            var value = data[index];
            var luma = !float.IsFinite(value)
                ? (ushort) 0
                : (ushort) Math.Clamp(MathF.Round(greyBase + value * greyPerUnit), 0.0f, 65535.0f);

            var x = index % width;
            var y = index / width;
            var outputX = y;
            var outputY = width - 1 - x;

            image[outputX, outputY] = new L16(luma);
        }

        image.SaveAsPng(path);
    }

    private static GeometryHeightmapPoint ToHeightmapPoint(FVector point)
    {
        return new GeometryHeightmapPoint(point.X, -point.Y, point.Z);
    }

    private static float Edge(float ax, float ay, float bx, float by, float px, float py)
    {
        return (px - ax) * (by - ay) - (py - ay) * (bx - ax);
    }

    private static bool IsTopLeft(float ax, float ay, float bx, float by)
    {
        return MathF.Abs(ay - by) <= EdgeEpsilon ? ax < bx : ay < by;
    }

    private readonly record struct GeometryHeightmapPoint(float X, float Y, float Z);

    private sealed record GeometryHeightmapRasterResult(
        float[] Data,
        int Width,
        int Height,
        float Scale,
        GeometryHeightmapBounds Bounds);

    private readonly record struct GeometryHeightmapRasterComponent(
        int Count,
        int MinX,
        int MaxX,
        int MinY,
        int MaxY)
    {
        public static GeometryHeightmapRasterComponent Empty => new(0, int.MaxValue, int.MinValue, int.MaxValue, int.MinValue);

        public GeometryHeightmapRasterComponent Include(int x, int y)
        {
            return new GeometryHeightmapRasterComponent(
                Count + 1,
                Math.Min(MinX, x),
                Math.Max(MaxX, x),
                Math.Min(MinY, y),
                Math.Max(MaxY, y));
        }
    }

    private sealed class GeometryHeightmapBounds
    {
        public float MinX = float.PositiveInfinity;
        public float MaxX = float.NegativeInfinity;
        public float MinY = float.PositiveInfinity;
        public float MaxY = float.NegativeInfinity;
        public float MinZ = float.PositiveInfinity;
        public float MaxZ = float.NegativeInfinity;

        public static GeometryHeightmapBounds Empty => new();

        public float AreaXY => Math.Max(MaxX - MinX, 0.0f) * Math.Max(MaxY - MinY, 0.0f);

        public bool IsValid =>
            float.IsFinite(MinX) &&
            float.IsFinite(MaxX) &&
            float.IsFinite(MinY) &&
            float.IsFinite(MaxY) &&
            float.IsFinite(MinZ) &&
            float.IsFinite(MaxZ);

        public void Include(GeometryHeightmapPoint point)
        {
            MinX = MathF.Min(MinX, point.X);
            MaxX = MathF.Max(MaxX, point.X);
            MinY = MathF.Min(MinY, point.Y);
            MaxY = MathF.Max(MaxY, point.Y);
            MinZ = MathF.Min(MinZ, point.Z);
            MaxZ = MathF.Max(MaxZ, point.Z);
        }

        public bool IntersectsXY(GeometryHeightmapBounds other)
        {
            return MaxX >= other.MinX &&
                   MinX <= other.MaxX &&
                   MaxY >= other.MinY &&
                   MinY <= other.MaxY;
        }

        public bool ApproximatelyEquals(GeometryHeightmapBounds other)
        {
            const float tolerance = 1.0f;
            return MathF.Abs(MinX - other.MinX) <= tolerance &&
                   MathF.Abs(MaxX - other.MaxX) <= tolerance &&
                   MathF.Abs(MinY - other.MinY) <= tolerance &&
                   MathF.Abs(MaxY - other.MaxY) <= tolerance;
        }
    }
}

