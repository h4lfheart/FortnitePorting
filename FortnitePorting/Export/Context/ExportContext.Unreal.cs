using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Models.Unreal.Lights;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.Fortnite;
using Serilog;

namespace FortnitePorting.Export.Context;

public partial class ExportContext
{
   public List<ExportMesh> World(UWorld world)
    {
        if (world.PersistentLevel.Load() is not ULevel level) return [];

        var actors = new List<ExportMesh>();
        
        actors.AddRangeIfNotNull(Level(level));

        foreach (var streamingLevelLazy in world.StreamingLevels)
        {
            if (streamingLevelLazy.Load() is not ULevelStreaming levelStreaming) continue;
            if (levelStreaming.WorldAsset.Load() is not UWorld worldAsset) continue;
            if (worldAsset.PersistentLevel.Load() is not ULevel streamingLevel) continue;
            
            actors.AddRangeIfNotNull(Level(streamingLevel));
        }

        var exportWorldPartition = Meta.WorldFlags.HasFlag(EWorldFlags.WorldPartitionGrids);
        var exportHLODs = Meta.WorldFlags.HasFlag(EWorldFlags.HLODs);
        if ((exportWorldPartition || exportHLODs) && level.GetOrDefault<UObject>("WorldSettings") is { } worldSettings
                                   && worldSettings.GetOrDefault<UObject>("WorldPartition") is { } worldPartition
                                   && worldPartition.GetOrDefault<UObject>("RuntimeHash") is { } runtimeHash)
        {
            if (exportWorldPartition)
                Meta.WorldFlags |= EWorldFlags.Actors;
            
            foreach (var streamingData in runtimeHash.GetOrDefault("RuntimeStreamingData", Array.Empty<FStructFallback>()))
            {
                var cells = new List<FPackageIndex>();
                cells.AddRange(streamingData.GetOrDefault("SpatiallyLoadedCells", Array.Empty<FPackageIndex>()));
                cells.AddRange(streamingData.GetOrDefault("NonSpatiallyLoadedCells", Array.Empty<FPackageIndex>()));

                foreach (var cell in cells)
                {
                    var gridCell = cell.Load();
                    var levelStreaming = gridCell?.GetOrDefault<UObject?>("LevelStreaming");
                    if (levelStreaming is null) continue;

                    var worldAsset = levelStreaming.Get<FSoftObjectPath>("WorldAsset");
                    var subWorld = worldAsset.Load<UWorld>();
                    var subLevel = subWorld.PersistentLevel.Load<ULevel>();
                    if (subLevel is null) continue;
                    
                    actors.AddRangeIfNotNull(Level(subLevel));
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
                    var subWorld = worldAsset.Load<UWorld>();
                    var subLevel = subWorld.PersistentLevel.Load<ULevel>();
                    if (subLevel is null) continue;
                    
                    actors.AddRangeIfNotNull(Level(subLevel));
                }

                break;
            }
        }

        return actors;
    }

    public List<ExportMesh> Level(ULevel level)
    {
        var actors = new List<ExportMesh>();
        var totalActors = level.Actors.Length;
        var currentActor = 0;
        
        foreach (var actorLazy in level.Actors)
        {
            currentActor++;
            if (actorLazy is null || actorLazy.IsNull) continue;

            var actor = actorLazy.Load();
            if (actor is null) continue;
            if (actor.ExportType == "LODActor") continue;
            if (actor.Name.StartsWith("Device_") 
                || actor.Name.StartsWith("VerseDevice_")
                || actor.Name.StartsWith("BP_Device_")) continue;

            Log.Information("Processing {ActorName}: {CurrentActor}/{TotalActors}", actor.Name, currentActor, totalActors);
            Meta.OnUpdateProgress(actor.Name, currentActor, totalActors);
            actors.AddRangeIfNotNull(Actor(actor));

            if (currentActor % 250 == 0)
            {
                GC.Collect();
            }
        }

        GC.Collect();
        return actors;
    } 

    public List<ExportMesh> Actor(UObject actor, bool loadTemplate = true)
    {
        var meshes = new List<ExportMesh>();

        if (Meta.WorldFlags.HasFlag(EWorldFlags.Actors))
        {
            if (Meta.WorldFlags.HasFlag(EWorldFlags.InstancedFoliage) && actor.TryGetValue(out FPackageIndex[] instanceComponents, "InstanceComponents"))
            {
                foreach (var instanceComponentLazy in instanceComponents)
                {
                    var instanceComponent = instanceComponentLazy.Load<UInstancedStaticMeshComponent>();
                    if (instanceComponent is null) continue;
                    if (instanceComponent.ExportType == "HLODInstancedStaticMeshComponent") continue;
                    
                    var exportMesh = MeshComponent(instanceComponent);
                    if (exportMesh is null) continue;
                    
                    var instanceTransform = instanceComponent.GetAbsoluteTransform();
                    
                    exportMesh.Location = instanceTransform.Translation;
                    exportMesh.Rotation = instanceTransform.Rotator();
                    exportMesh.Scale = instanceTransform.Scale3D;

                    meshes.Add(exportMesh);
                }
            }
            
            if (actor.TryGetValue(out UStaticMeshComponent staticMeshComponent, "StaticMeshComponent", "StaticMesh", "Mesh", "LightMesh"))
            {
                var exportMesh = MeshComponent(staticMeshComponent) ?? new ExportMesh { IsEmpty = true };
                exportMesh.Name = actor.Name;
                exportMesh.Location = staticMeshComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector);
                exportMesh.Rotation = staticMeshComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator);
                exportMesh.Scale = staticMeshComponent.GetOrDefault("RelativeScale3D", FVector.OneVector);

                foreach (var extraMesh in ExtraActorMeshes(actor))
                {
                    exportMesh.Children.AddIfNotNull(extraMesh);
                }

                actor.GatherTemplateProperties();
                var textureDatas = actor.GetAllProperties<UBuildingTextureData>("TextureData");

                foreach (var (textureData, index) in textureDatas)
                {
                    exportMesh.TextureData.AddIfNotNull(TextureData(textureData, index));
                }
                            
                if (actor.TryGetValue(out FSoftObjectPath[] additionalWorlds, "AdditionalWorlds"))
                {
                    foreach (var additionalWorldPath in additionalWorlds)
                    {
                        if (additionalWorldPath.TryLoad<UWorld>(out var world))
                        {
                            exportMesh.Children.AddRange(World(world));
                        }
                    }
                }
                
                if (loadTemplate && actor.Template?.Load() is { } template)
                {
                    var basePath = template.GetPathName().SubstringBeforeLast(".");
                    var blueprintPath = $"{basePath}.{basePath.SubstringAfterLast("/")}_C";
                    if (CUE4ParseVM.Provider.TryLoadPackageObject(blueprintPath, out var templateBlueprintGeneratedClass))
                    {
                        exportMesh.AddChildren(ConstructionScript(templateBlueprintGeneratedClass));
                        exportMesh.AddChildren(InheritableComponentHandler(templateBlueprintGeneratedClass));
                    }
                }

                meshes.Add(exportMesh);
            }
            
            if (actor.TryGetValue(out USkeletalMeshComponent skeletalMeshComponent, "SkeletalMeshComponent", "SkeletalMesh"))
            {
                var exportMesh = MeshComponent(skeletalMeshComponent) ?? new ExportMesh { IsEmpty = true };
                exportMesh.Name = actor.Name;
                exportMesh.Location = skeletalMeshComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector);
                exportMesh.Rotation = skeletalMeshComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator);
                exportMesh.Scale = skeletalMeshComponent.GetOrDefault("RelativeScale3D", FVector.OneVector);

                foreach (var extraMesh in ExtraActorMeshes(actor))
                {
                    exportMesh.Children.AddIfNotNull(extraMesh);
                }
                
                if (loadTemplate && actor.Template?.Load() is { } template)
                {
                    var basePath = template.GetPathName().SubstringBeforeLast(".");
                    var blueprintPath = $"{basePath}.{basePath.SubstringAfterLast("/")}_C";
                    if (CUE4ParseVM.Provider.TryLoadPackageObject(blueprintPath, out var templateBlueprintGeneratedClass))
                    {
                        exportMesh.AddChildren(ConstructionScript(templateBlueprintGeneratedClass));
                        exportMesh.AddChildren(InheritableComponentHandler(templateBlueprintGeneratedClass));
                    }
                }

                meshes.Add(exportMesh);
            }
            
            // extra meshes i.e. doors and such
            var targetMesh = meshes.FirstOrDefault();
            foreach (var extraMesh in ExtraActorMeshes(actor))
            {
                targetMesh?.Children.AddIfNotNull(extraMesh);
            }

        }

        if (Meta.WorldFlags.HasFlag(EWorldFlags.Landscape) && actor is ALandscapeProxy landscapeProxy && landscapeProxy.ExportType != "Landscape")
        {
            var transform = landscapeProxy.GetAbsoluteTransformFromRootComponent();
            
            var exportMesh = new ExportMesh();
            exportMesh.Name = landscapeProxy.Name;
            exportMesh.Path = Export(landscapeProxy, embeddedAsset: true, synchronousExport: true);
            exportMesh.Location = transform.Translation;
            exportMesh.Scale = transform.Scale3D;
            meshes.Add(exportMesh);
        }

        if (Meta.WorldFlags.HasFlag(EWorldFlags.HLODs))
        {
            if (actor.ExportType == "FortMainHLOD")
            {
                var instanceComponents = actor.GetOrDefault<FPackageIndex[]>("InstanceComponents", []);
                foreach (var instanceComponentLazy in instanceComponents)
                {
                    var instanceComponent = instanceComponentLazy.Load<USceneComponent>();
                    if (instanceComponent is null) continue;
                    
                    var component = MeshComponent(instanceComponent);
                    if (component is null) continue;

                    var transform = instanceComponent.GetAbsoluteTransform();
                    component.Location = transform.Translation;
                    component.Rotation = transform.Rotation.Rotator();
                    component.Scale = transform.Scale3D;
                    meshes.AddIfNotNull(component);
                }
            }
        }

        return meshes;
    }

    public List<ExportObject> Blueprint(UBlueprintGeneratedClass blueprintGeneratedClass)
    {
        var objects = new List<ExportObject>();
        
        objects.AddRangeIfNotNull(ConstructionScript(blueprintGeneratedClass));
        objects.AddRangeIfNotNull(InheritableComponentHandler(blueprintGeneratedClass));

        if (blueprintGeneratedClass.ClassDefaultObject != null && blueprintGeneratedClass.ClassDefaultObject.TryLoad(out var classDefaultObject))
        {
            objects.AddRangeIfNotNull(Actor(classDefaultObject!, loadTemplate: false));
        }
        
        return objects;
    }

    public List<ExportObject> ConstructionScript(UObject blueprint)
    {
        if (!blueprint.TryGetValue(out UObject constructionScript, "SimpleConstructionScript")) return [];
        
        var objects = new List<ExportObject>();
        
        var allNodes = constructionScript.GetOrDefault("AllNodes", Array.Empty<UObject>());
        foreach (var node in allNodes)
        {
            var componentTemplate = node.GetOrDefault<UObject>("ComponentTemplate");
            if (componentTemplate is UInstancedStaticMeshComponent instancedStaticMeshComponent)
            {
                objects.AddIfNotNull(MeshComponent(instancedStaticMeshComponent));
            }
            else if (componentTemplate is UStaticMeshComponent subStaticMeshComponent)
            {
                objects.AddIfNotNull(MeshComponent(subStaticMeshComponent));
            }
            else if (componentTemplate is ULightComponentBase pointLightComponent)
            {
                objects.AddIfNotNull(LightComponent(pointLightComponent));
            }
        }

        return objects;
    }
    
    public List<ExportObject> InheritableComponentHandler(UObject blueprint)
    {
        if (!blueprint.TryGetValue(out UObject inheritableComponentHandler, "InheritableComponentHandler")) return [];
        
        var objects = new List<ExportObject>();
        
        var records = inheritableComponentHandler.GetOrDefault("Records", Array.Empty<FStructFallback>());
        foreach (var record in records)
        {
            var componentTemplate = record.GetOrDefault<UObject>("ComponentTemplate");
            if (componentTemplate is UInstancedStaticMeshComponent instancedStaticMeshComponent)
            {
                objects.AddIfNotNull(MeshComponent(instancedStaticMeshComponent));
            }
            else if (componentTemplate is UStaticMeshComponent subStaticMeshComponent)
            {
                objects.AddIfNotNull(MeshComponent(subStaticMeshComponent));
            }
            else if (componentTemplate is ULightComponentBase pointLightComponent)
            {
                objects.AddIfNotNull(LightComponent(pointLightComponent));
            }
        }

        return objects;
    }

    public ExportLight? LightComponent(ULightComponentBase lightComponent)
    {
        lightComponent.GatherTemplateProperties();
        return lightComponent switch
        {
            UPointLightComponent pointLightComponent => LightComponent(pointLightComponent),
            _ => null
        };
    }

    public ExportLight LightComponent(UPointLightComponent pointLightComponent)
    {
        return new ExportPointLight
        {
            Name = pointLightComponent.Name,
            Location = pointLightComponent.RelativeLocation,
            Rotation = pointLightComponent.RelativeRotation,
            Scale = pointLightComponent.RelativeScale3D,
            Intensity = pointLightComponent.Intensity,
            Color = pointLightComponent.LightColor.ToLinearColor(),
            CastShadows = pointLightComponent.CastShadows,
            AttenuationRadius = pointLightComponent.AttenuationRadius,
            Radius = pointLightComponent.SourceRadius
        };
    }
}