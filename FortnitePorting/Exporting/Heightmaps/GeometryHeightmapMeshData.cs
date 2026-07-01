using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Models.CUE4Parse;
using Newtonsoft.Json;

namespace FortnitePorting.Exporting.Heightmaps;

public sealed record GeometryHeightmapMeshGeometry(FVector[] Vertices, uint[] Indices);

public sealed record GeometryHeightmapMeshInstance(
    string Name,
    string Path,
    GeometryHeightmapMeshGeometry Geometry,
    FTransform Transform,
    bool IsTerrain);

public sealed record GeometryHeightmapFilteredMeshSample(
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("path")] string Path,
    [property: JsonProperty("rule")] string Rule);

public sealed class GeometryHeightmapFilterSummary
{
    private const int MaxSamples = 40;

    public int Total { get; private set; }
    public Dictionary<string, int> Rules { get; } = [];
    public List<GeometryHeightmapFilteredMeshSample> Samples { get; } = [];

    public void Add(string rule, string name, string path)
    {
        Total++;
        Rules[rule] = Rules.GetValueOrDefault(rule) + 1;

        if (Samples.Count >= MaxSamples || Samples.Any(sample => sample.Path == path && sample.Rule == rule))
            return;

        Samples.Add(new GeometryHeightmapFilteredMeshSample(name, path, rule));
    }
}

public static class GeometryHeightmapSpawnIslandDetector
{
    private static readonly string[] Keywords =
    [
        "spawnisland",
        "spawn_island",
        "spawn-island",
        "sourspawn",
        "starterisland",
        "starter_island",
        "warmupisland",
        "warmup_island",
        "pregameisland",
        "pregame_island"
    ];

    public static bool IsSpawnIsland(string name, string path)
    {
        return Keywords.Any(keyword =>
            name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            path.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class GeometryHeightmapMeshGeometryCache
{
    private readonly Dictionary<string, GeometryHeightmapMeshGeometry> _geometries = [];

    public GeometryHeightmapMeshGeometry GetOrCreate(UObject source)
    {
        var key = source.GetPathName();
        if (_geometries.TryGetValue(key, out var geometry))
            return geometry;

        geometry = CreateGeometry(source) ?? throw new InvalidOperationException($"Failed to convert {source.Name} to mesh geometry.");
        _geometries[key] = geometry;
        return geometry;
    }

    private static GeometryHeightmapMeshGeometry? CreateGeometry(UObject source)
    {
        switch (source)
        {
            case UStaticMesh staticMesh:
            {
                if (!staticMesh.TryConvert(out var convertedMesh, out _, ENaniteMeshFormat.OnlyNormalLODs))
                    return null;

                try
                {
                    return FromStaticMesh(convertedMesh);
                }
                finally
                {
                    convertedMesh.Dispose();
                }
            }
            case USkeletalMesh skeletalMesh:
            {
                if (!skeletalMesh.TryConvert(out var convertedMesh))
                    return null;

                try
                {
                    return FromSkeletalMesh(convertedMesh);
                }
                finally
                {
                    convertedMesh.Dispose();
                }
            }
            case USplineMeshComponent splineMesh:
            {
                if (!splineMesh.TryConvert(out var convertedMesh, out _, ENaniteMeshFormat.OnlyNormalLODs))
                    return null;

                try
                {
                    return FromStaticMesh(convertedMesh);
                }
                finally
                {
                    convertedMesh.Dispose();
                }
            }
            case ALandscapeProxy landscapeProxy:
            {
                var convertedMesh = new LandscapeProcessor(landscapeProxy).Process();
                try
                {
                    return FromStaticMesh(convertedMesh);
                }
                finally
                {
                    convertedMesh.Dispose();
                }
            }
            default:
                return null;
        }
    }

    private static GeometryHeightmapMeshGeometry? FromStaticMesh(CStaticMesh mesh)
    {
        var lod = mesh.LODs.FirstOrDefault(lod => lod is { Verts: not null, Indices: not null } && !lod.SkipLod);
        if (lod?.Verts is null || lod.Indices is null)
            return null;

        return new GeometryHeightmapMeshGeometry(
            lod.Verts.Select(vertex => vertex.Position).ToArray(),
            lod.Indices.Value.ToArray());
    }

    private static GeometryHeightmapMeshGeometry? FromSkeletalMesh(CSkeletalMesh mesh)
    {
        var lod = mesh.LODs.FirstOrDefault(lod => lod is { Verts: not null, Indices: not null } && !lod.SkipLod);
        if (lod?.Verts is null || lod.Indices is null)
            return null;

        return new GeometryHeightmapMeshGeometry(
            lod.Verts.Select(vertex => vertex.Position).ToArray(),
            lod.Indices.Value.ToArray());
    }
}

internal static class GeometryHeightmapMeshFilter
{
    private static readonly GeometryHeightmapMeshFilterRule[] Rules =
    [
        new(
            "decal_visual_mesh",
            nameKeywords:
            [
                "decal"
            ],
            pathFragments:
            [
                "decal"
            ]),
        new(
            "foliage_detail_mesh",
            nameKeywords:
            [
                "bush",
                "plant",
                "wavygrass",
                "shrub",
                "flowers"
            ]),
        new(
            "flat_visual_sheet",
            nameKeywords:
            [
                "projectioncube",
                "waterplane",
                "ocean_floor",
                "daisy_ocean",
                "icesheet",
                "groundfog",
                "fogsheet",
                "skullroomfog"
            ],
            pathFragments:
            [
                "/Engine/BasicShapes/Plane.Plane",
                "/Game/Creative/Environments/Meshes/CP_Ground_Plane"
            ]),
        new(
            "background_landscape_mesh",
            pathFragments:
            [
                "/Environments/Landscape/Meshes/Background"
            ]),
        new(
            "flying_orb_visual_effect",
            nameKeywords:
            [
                "dragoncart_orb",
                "flyingorb"
            ],
            pathFragments:
            [
                "/DragonCartNarrative/Assets/FlyingOrb/"
            ])
    ];

    public static string? GetCullRule(string name, string path, bool isTerrain)
    {
        if (isTerrain)
            return null;

        var meshName = path.Split('/').LastOrDefault() ?? name;
        foreach (var rule in Rules)
        {
            if (rule.NameKeywords.Any(keyword =>
                    meshName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return rule.Name;
            }

            if (rule.PathFragments.Any(fragment => path.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                return rule.Name;
        }

        return null;
    }

    private sealed class GeometryHeightmapMeshFilterRule(
        string name,
        string[]? nameKeywords = null,
        string[]? pathFragments = null)
    {
        public string Name { get; } = name;
        public string[] NameKeywords { get; } = nameKeywords ?? [];
        public string[] PathFragments { get; } = pathFragments ?? [];
    }
}
