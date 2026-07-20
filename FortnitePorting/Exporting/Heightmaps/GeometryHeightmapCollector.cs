using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared.Extensions;
using Serilog;

namespace FortnitePorting.Exporting.Heightmaps;

public sealed class GeometryHeightmapCollector
{
    private static readonly string[] SpawnIslandKeywords =
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

    private readonly EWorldFlags _worldFlags;
    private readonly bool _includeSpawnIsland;
    private readonly GeometryHeightmapMeshGeometryCache _geometryCache = new();
    private readonly IProgress<GeometryHeightmapProgress>? _progress;
    private readonly CancellationToken _cancellationToken;

    public GeometryHeightmapCollector(
        EWorldFlags worldFlags,
        bool includeSpawnIsland = false,
        IProgress<GeometryHeightmapProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _worldFlags = worldFlags;
        _includeSpawnIsland = includeSpawnIsland;
        _progress = progress;
        _cancellationToken = cancellationToken;
    }

    public static bool IsSpawnIsland(string name, string path)
    {
        return SpawnIslandKeywords.Any(keyword =>
            name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            path.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public List<GeometryHeightmapMeshInstance> Collect(IEnumerable<UWorld> worlds, bool includeStreamingLevels = true)
    {
        var instances = new List<GeometryHeightmapMeshInstance>();
        foreach (var world in worlds)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            CollectWorld(world, instances, includeStreamingLevels);
        }

        return instances;
    }

    private void CollectWorld(UWorld world, List<GeometryHeightmapMeshInstance> instances,
        bool includeStreamingLevels = true)
    {
        if (world.PersistentLevel.Load() is ULevel level)
            CollectLevel(level, instances);

        if (!includeStreamingLevels)
            return;

        foreach (var streamingLevelLazy in world.StreamingLevels)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (streamingLevelLazy.Load() is not ULevelStreaming levelStreaming) continue;
            if (levelStreaming.WorldAsset?.Load() is not UWorld worldAsset) continue;
            if (worldAsset.PersistentLevel.Load() is not ULevel streamingLevel) continue;

            CollectLevel(streamingLevel, instances);
        }
    }

    private void CollectLevel(ULevel level, List<GeometryHeightmapMeshInstance> instances)
    {
        var totalActors = level.Actors.Length;
        var currentActor = 0;

        foreach (var actorLazy in level.Actors)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            currentActor++;
            if (actorLazy is null || actorLazy.IsNull) continue;

            var actor = actorLazy.Load();
            if (actor is null) continue;
            if (actor.ExportType == "LODActor") continue;
            if (actor.Name.StartsWith("Device_") ||
                actor.Name.StartsWith("VerseDevice_") ||
                actor.Name.StartsWith("BP_Device_"))
            {
                continue;
            }

            _progress?.Report(new GeometryHeightmapProgress($"Collecting {actor.Name}", currentActor, totalActors));
            CollectActor(actor, instances);
        }
    }

    private void CollectActor(UObject actor, List<GeometryHeightmapMeshInstance> instances, bool loadTemplate = true)
    {
        if (_worldFlags.HasFlag(EWorldFlags.Actors))
        {
            var hasMesh = false;

            if (actor.TryGetValue(out UStaticMeshComponent staticMeshComponent, "StaticMeshComponent", "StaticMesh",
                    "Mesh", "LightMesh"))
            {
                CollectMeshComponent(staticMeshComponent, instances, actor.Name);
                CollectAdditionalWorlds(actor, instances);
                hasMesh = true;
            }

            if (actor.TryGetValue(out USkeletalMeshComponent skeletalMeshComponent, "SkeletalMeshComponent",
                    "SkeletalMesh"))
            {
                CollectMeshComponent(skeletalMeshComponent, instances, actor.Name);
                hasMesh = true;
            }

            if (hasMesh)
            {
                CollectExtraActorMeshes(actor, instances);

                if (loadTemplate)
                    CollectActorTemplate(actor, instances);
            }
        }

        if (_worldFlags.HasFlag(EWorldFlags.Landscape) &&
            actor is ALandscapeProxy landscapeProxy &&
            landscapeProxy.LandscapeComponents.Length > 0)
        {
            var transform = landscapeProxy.GetAbsoluteTransformFromRootComponent();
            AddMeshInstance(landscapeProxy.Name, landscapeProxy, transform, true, instances);
        }

        if (_worldFlags.HasFlag(EWorldFlags.HLODs) && actor.ExportType == "FortMainHLOD")
        {
            var instanceComponents = actor.GetOrDefault<FPackageIndex[]>("InstanceComponents", []);
            foreach (var instanceComponentLazy in instanceComponents)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var instanceComponent = instanceComponentLazy.Load<USceneComponent>();
                if (instanceComponent is null) continue;

                CollectMeshComponent(instanceComponent, instances);
            }
        }
    }

    private void CollectActorTemplate(UObject actor, List<GeometryHeightmapMeshInstance> instances)
    {
        if (actor.Template?.Load() is not { } template)
            return;

        var basePath = template.GetPathName().SubstringBeforeLast(".");
        var blueprintPath = $"{basePath}.{basePath.SubstringAfterLast("/")}_C";
        if (!UEParse.Provider.TryLoadPackageObject(blueprintPath, out var blueprintGeneratedClass))
            return;

        CollectBlueprintComponents(blueprintGeneratedClass, instances, actor.GetAbsoluteTransformFromRootComponent());
    }

    private void CollectBlueprintComponents(UObject blueprint, List<GeometryHeightmapMeshInstance> instances,
        FTransform parentTransform)
    {
        if (blueprint.TryGetValue(out UObject constructionScript, "SimpleConstructionScript"))
        {
            foreach (var node in constructionScript.GetOrDefault("AllNodes", Array.Empty<UObject>()))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                CollectTemplateComponent(node.GetOrDefault<UObject>("ComponentTemplate"), instances, parentTransform);
            }
        }

        if (blueprint.TryGetValue(out UObject inheritableComponentHandler, "InheritableComponentHandler"))
        {
            foreach (var record in inheritableComponentHandler.GetOrDefault("Records", Array.Empty<FStructFallback>()))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                CollectTemplateComponent(record.GetOrDefault<UObject>("ComponentTemplate"), instances, parentTransform);
            }
        }
    }

    private void CollectTemplateComponent(UObject? componentTemplate, List<GeometryHeightmapMeshInstance> instances,
        FTransform parentTransform)
    {
        switch (componentTemplate)
        {
            case UInstancedStaticMeshComponent instancedStaticMeshComponent:
                CollectMeshComponent(instancedStaticMeshComponent, instances, parentTransform: parentTransform);
                break;
            case UStaticMeshComponent staticMeshComponent:
                CollectMeshComponent(staticMeshComponent, instances, parentTransform: parentTransform);
                break;
            case USkeletalMeshComponent skeletalMeshComponent:
                CollectMeshComponent(skeletalMeshComponent, instances, parentTransform: parentTransform);
                break;
        }
    }

    private void CollectExtraActorMeshes(UObject actor, List<GeometryHeightmapMeshInstance> instances)
    {
        actor.GatherTemplateProperties();

        foreach (var staticMeshComponent in actor.GetDataListItems<UStaticMeshComponent>("StaticMeshComponent", "Mesh"))
        {
            _cancellationToken.ThrowIfCancellationRequested();
            CollectMeshComponent(staticMeshComponent, instances);
        }
    }

    private void CollectAdditionalWorlds(UObject actor, List<GeometryHeightmapMeshInstance> instances)
    {
        if (!actor.TryGetValue(out FSoftObjectPath[] additionalWorlds, "AdditionalWorlds"))
            return;

        foreach (var additionalWorldPath in additionalWorlds)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (additionalWorldPath.TryLoad<UWorld>(out var world))
                CollectWorld(world, instances);
        }
    }

    private void CollectMeshComponent(USceneComponent sceneComponent, List<GeometryHeightmapMeshInstance> instances,
        string? displayName = null, FTransform? parentTransform = null)
    {
        switch (sceneComponent)
        {
            case UInstancedStaticMeshComponent instancedStaticMeshComponent:
                CollectMeshComponent(instancedStaticMeshComponent, instances, displayName, parentTransform);
                break;
            case USplineMeshComponent splineMeshComponent:
                CollectSplineMeshComponent(splineMeshComponent, instances, displayName, parentTransform);
                break;
            case UStaticMeshComponent staticMeshComponent:
                CollectMeshComponent(staticMeshComponent, instances, displayName, parentTransform);
                break;
            case USkeletalMeshComponent skeletalMeshComponent:
                CollectMeshComponent(skeletalMeshComponent, instances, displayName, parentTransform);
                break;
        }
    }

    private void CollectMeshComponent(UStaticMeshComponent meshComponent, List<GeometryHeightmapMeshInstance> instances,
        string? displayName = null, FTransform? parentTransform = null)
    {
        if (meshComponent is USplineMeshComponent splineMeshComponent)
        {
            CollectSplineMeshComponent(splineMeshComponent, instances, displayName, parentTransform);
            return;
        }

        var mesh = meshComponent.GetStaticMesh().Load<UStaticMesh>();
        if (mesh is null) return;

        AddMeshInstance(displayName ?? mesh.Name, mesh,
            ApplyParent(meshComponent.GetAbsoluteTransform(), parentTransform), false, instances);
    }

    private void CollectMeshComponent(USkeletalMeshComponent meshComponent,
        List<GeometryHeightmapMeshInstance> instances, string? displayName = null, FTransform? parentTransform = null)
    {
        var mesh = meshComponent.GetSkeletalMesh().Load<USkeletalMesh>();
        if (mesh is null) return;

        AddMeshInstance(displayName ?? mesh.Name, mesh,
            ApplyParent(meshComponent.GetAbsoluteTransform(), parentTransform), false, instances);
    }

    private void CollectMeshComponent(UInstancedStaticMeshComponent instanceComponent,
        List<GeometryHeightmapMeshInstance> instances, string? displayName = null, FTransform? parentTransform = null)
    {
        var mesh = instanceComponent.GetOrDefault<UStaticMesh?>("StaticMesh");
        if (mesh is null) return;

        var componentTransform = ApplyParent(instanceComponent.GetAbsoluteTransform(), parentTransform);
        foreach (var instance in instanceComponent.PerInstanceSMData ?? [])
        {
            _cancellationToken.ThrowIfCancellationRequested();
            AddMeshInstance(
                $"{displayName ?? mesh.Name}_Instance",
                mesh,
                instance.TransformData * componentTransform,
                false,
                instances);
        }
    }

    private void CollectSplineMeshComponent(USplineMeshComponent meshComponent,
        List<GeometryHeightmapMeshInstance> instances, string? displayName = null, FTransform? parentTransform = null)
    {
        AddMeshInstance(displayName ?? meshComponent.Name, meshComponent,
            ApplyParent(meshComponent.GetAbsoluteTransform(), parentTransform), false, instances);
    }

    private static FTransform ApplyParent(FTransform transform, FTransform? parentTransform)
    {
        return parentTransform is { } parent ? transform * parent : transform;
    }

    private void AddMeshInstance(
        string name,
        UObject source,
        FTransform transform,
        bool isTerrain,
        List<GeometryHeightmapMeshInstance> instances)
    {
        try
        {
            var path = source.GetPathName();
            if (!_includeSpawnIsland && IsSpawnIsland(name, path))
                return;

            if (GeometryHeightmapMeshFilter.GetCullRule(name, path, isTerrain) is { } _)
                return;

            var geometry = _geometryCache.GetOrCreate(source);
            instances.Add(new GeometryHeightmapMeshInstance(name, path, geometry, transform, isTerrain));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Skipping unsupported geometry heightmap mesh {MeshName}", name);
        }
    }

    private sealed class GeometryHeightmapMeshGeometryCache
    {
        private readonly Dictionary<string, GeometryHeightmapMeshGeometry> _geometries = [];

        public GeometryHeightmapMeshGeometry GetOrCreate(UObject source)
        {
            var key = source.GetPathName();
            if (_geometries.TryGetValue(key, out var geometry))
                return geometry;

            geometry = CreateGeometry(source) ??
                       throw new InvalidOperationException($"Failed to convert {source.Name} to mesh geometry.");
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

    private static class GeometryHeightmapMeshFilter
    {
        private static readonly Rule[] Rules =
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
            return Rules
                .FirstOrDefault(rule =>
                    rule.NameKeywords.Any(keyword =>
                        meshName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    rule.PathFragments.Any(fragment =>
                        path.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                ?.Name;
        }

        private sealed class Rule(
            string name,
            string[]? nameKeywords = null,
            string[]? pathFragments = null)
        {
            public string Name { get; } = name;
            public string[] NameKeywords { get; } = nameKeywords ?? [];
            public string[] PathFragments { get; } = pathFragments ?? [];
        }
    }
}