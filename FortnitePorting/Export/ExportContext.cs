using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using FFMpegCore;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Models.Unreal.Lights;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;

using FortnitePorting.Shared.Models.Fortnite;
using FortnitePorting.Shared.Services;
using Serilog;
using SixLabors.ImageSharp;
using SkiaSharp;
using Image = System.Drawing.Image;

namespace FortnitePorting.Export;

public class ExportContext
{
    public List<Task> ExportTasks = [];
    private HashSet<ExportMaterial> MaterialCache = [];

    public readonly ExportDataMeta Meta;
    private readonly ExporterOptions FileExportOptions;

    public ExportContext(ExportDataMeta metaData)
    {
        Meta = metaData;
        FileExportOptions = Meta.Settings.CreateExportOptions();
    }
    
    public ExportPart? CharacterPart(UObject part)
    {
        var skeletalMesh = part.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
        if (skeletalMesh is null) return null;

        var exportPart = Mesh<ExportPart>(skeletalMesh);
        if (exportPart is null) return null;
        
        exportPart.Type = part.GetOrDefault("CharacterPartType", EFortCustomPartType.Head);
        exportPart.GenderPermitted = part.GetOrDefault("GenderPermitted", EFortCustomGender.Male);

        if (part.TryGetValue(out FStructFallback[] materialOverrides, "MaterialOverrides"))
        {
            foreach (var material in materialOverrides)
            {
                exportPart.OverrideMaterials.AddIfNotNull(OverrideMaterial(material));
            }
        }

        if (part.TryGetValue(out UObject additionalData, "AdditionalData"))
        {
            switch (additionalData.ExportType)
            {
                case "CustomCharacterHeadData":
                {
                    var meta = new ExportHeadMeta();

                    foreach (var type in Enum.GetValues<ECustomHatType>())
                        if (additionalData.TryGetValue(out FName[] morphNames, type + "MorphTargets"))
                            meta.MorphNames[type] = morphNames.First().Text;

                    if (additionalData.TryGetValue(out UObject skinColorSwatch, "SkinColorSwatch"))
                    {
                        var colorPairs = skinColorSwatch.GetOrDefault("ColorPairs", Array.Empty<FStructFallback>());
                        var skinColorPair = colorPairs.FirstOrDefault(x => x.Get<FName>("ColorName").Text.Equals("Skin Boost Color and Exponent", StringComparison.OrdinalIgnoreCase));
                        if (skinColorPair is not null) meta.SkinColor = skinColorPair.Get<FLinearColor>("ColorValue");
                    }

                    if (additionalData.TryGetValue(out UAnimBlueprintGeneratedClass animBlueprint, "AnimClass"))
                    {
                        var animBlueprintData = animBlueprint.ClassDefaultObject.Load()!;
                        if (animBlueprintData.TryGetValue(out FStructFallback poseAssetNode, "AnimGraphNode_PoseBlendNode"))
                        {
                            PoseAsset(poseAssetNode.Get<UPoseAsset>("PoseAsset"), meta);
                        }
                        else if (skeletalMesh.ReferenceSkeleton.FinalRefBoneInfo.Any(bone => bone.Name.Text.Equals("FACIAL_C_FacialRoot", StringComparison.OrdinalIgnoreCase))
                                 && CUE4ParseVM.Provider.TryLoadObject("/BRCosmetics/Characters/Player/Male/Medium/Heads/M_MED_Jonesy3L_Head/Meshes/3L/3L_lod2_Facial_Poses_PoseAsset", out UPoseAsset poseAsset))
                        {
                            PoseAsset(poseAsset, meta);
                        }
                    }

                    exportPart.Meta = meta;
                    break;
                }
                case "CustomCharacterHatData":
                {
                    var meta = new ExportHatMeta
                    {
                        AttachToSocket = part.GetOrDefault("bAttachToSocket", true),
                        Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text
                    };

                    if (additionalData.TryGetValue(out FName hatType, "HatType")) meta.HatType = hatType.Text.Replace("ECustomHatType::ECustomHatType_", string.Empty);
                    exportPart.Meta = meta;
                    break;
                }
                case "CustomCharacterCharmData":
                {
                    var meta = new ExportAttachMeta
                    {
                        AttachToSocket = part.GetOrDefault("bAttachToSocket", true),
                        Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text
                    };
                    exportPart.Meta = meta;
                    break;
                }
                case "CustomCharacterBodyPartData":
                {
                    var masterSkeletalMeshes = part.GetOrDefault("MasterSkeletalMeshes", Array.Empty<FSoftObjectPath>());
                    var masterSkeletalMesh = masterSkeletalMeshes
                        .Select(index => index.LoadOrDefault<USkeletalMesh>())
                        .FirstOrDefault(mesh => mesh is not null);
                    
                    if (masterSkeletalMesh is null) break;

                    var meta = new ExportMasterSkeletonMeta
                    {
                        MasterSkeletalMesh = Mesh(masterSkeletalMesh)
                    };
                    exportPart.Meta = meta;
                    break;
                }
            }
        }
        
        return exportPart;
    }
    
    public void PoseAsset(UPoseAsset poseAsset, ExportPoseDataMeta meta)
    {
        /* Only tested when bAdditivePose = true */
        if (!poseAsset.bAdditivePose)
        {
            Log.Warning($"{poseAsset.Name}: bAdditivePose = false is unsupported");
            return;
        }

        var poseContainer = poseAsset.PoseContainer;
        var poses = poseContainer.Poses;
        if (poses.Length == 0)
        {
            Log.Warning($"{poseAsset.Name}: has no poses");
            return;
        }

        var poseNames = poseContainer.GetPoseNames().ToArray();
        if (poseNames is null || poseNames.Length == 0)
        {
            Log.Warning($"{poseAsset.Name}: PoseFNames is null or empty");
            return;
        }

        /* Assert number of tracks == number of bones for given skeleton */
        var poseTracks = poseContainer.Tracks;

        if (poseContainer.TrackPoseInfluenceIndices is not null)
        {
            var poseTrackInfluences = poseContainer.TrackPoseInfluenceIndices;
            if (poseTracks.Length != poseTrackInfluences.Length)
            {
                Log.Warning($"{poseAsset.Name}: length of Tracks != length of TrackPoseInfluenceIndices");
                return;
            }

            /* Add poses by name first in order they appear */
            for (var i = 0; i < poses.Length; i++)
            {
                var pose = poses[i];
                var poseName = poseNames[i];
                var poseData = new PoseData(poseName, pose.CurveData);
                meta.PoseData.Add(poseData);
            }

            /* Discover connection between bone name and relative location. */
            for (var i = 0; i < poseTrackInfluences.Length; i++)
            {
                var poseTrackInfluence = poseTrackInfluences[i];
                if (poseTrackInfluence is null) continue;

                var poseTrackName = poseTracks[i];
                foreach (var influence in poseTrackInfluence.Influences)
                {
                    var pose = meta.PoseData[influence.PoseIndex];
                    var transform = poses[influence.PoseIndex].LocalSpacePose[influence.BoneTransformIndex];
                    if (!transform.Rotation.IsNormalized)
                        transform.Rotation.Normalize();

                    pose.Keys.Add(new PoseKey(
                        poseTrackName.PlainText, /* Bone name to move */
                        transform.Translation,
                        transform.Rotation,
                        transform.Scale3D,
                        influence.PoseIndex,
                        influence.BoneTransformIndex
                    ));
                }
            }
        }
        else
        {
            /* Add poses by name first in order they appear */
            for (var i = 0; i < poses.Length; i++)
            {
                var pose = poses[i];
                var poseName = poseNames[i];
                var poseData = new PoseData(poseName, pose.CurveData);
                meta.PoseData.Add(poseData);
            }

            /* Discover connection between bone name and relative location. */
            for (var i = 0; i < poses.Length; i++)
            {
                var poseData = poses[i];

                foreach (var (trackIndex, bufferIndex) in poseData.TrackToBufferIndex)
                {
                    var transform = poseData.LocalSpacePose[bufferIndex];
                    if (!transform.Rotation.IsNormalized)
                        transform.Rotation.Normalize();

                    meta.PoseData[i].Keys.Add(new PoseKey(
                        poseTracks[trackIndex].PlainText, /* Bone name to move */
                        transform.Translation,
                        transform.Rotation,
                        transform.Scale3D,
                        trackIndex,
                        bufferIndex
                    ));
                }
            }
        }
    }
    
    public List<ExportMesh> WeaponDefinition(UObject weaponDefinition)
    {
        var weaponMeshes = WeaponDefinitionMeshes(weaponDefinition);
        var exportWeapons = new List<ExportMesh>();
        foreach (var weaponMesh in weaponMeshes)
        {
            exportWeapons.AddIfNotNull(Mesh(weaponMesh));
        }
        
        if (exportWeapons.FirstOrDefault() is { } targetMesh 
            && weaponDefinition.GetDataListItem<FSoftObjectPath[]>("WeaponMaterialOverrides") is { } materialOverrides)
        {
            var slot = 0;
            foreach (var materialOverridePath in materialOverrides)
            {
                var materialOverride = materialOverridePath.Load<UMaterialInterface>();
                targetMesh.OverrideMaterials.AddIfNotNull(Material(materialOverride, slot));
                slot++;
            }
        }

        return exportWeapons;
    }
    
    public static List<UObject> WeaponDefinitionMeshes(UObject weaponDefinition)
    {
        var exportWeapons = new List<UObject>();

        var skeletalMesh = weaponDefinition.GetOrDefault<USkeletalMesh?>("WeaponMeshOverride");
        skeletalMesh ??= weaponDefinition.GetOrDefault<USkeletalMesh?>("PickupSkeletalMesh");
        skeletalMesh ??= weaponDefinition.GetDataListItem<USkeletalMesh?>("WeaponMeshOverride");
        skeletalMesh ??= weaponDefinition.GetDataListItem<USkeletalMesh?>("PickupSkeletalMesh");
        exportWeapons.AddIfNotNull(skeletalMesh);

        var offhandSkeletalMesh = weaponDefinition.GetOrDefault<USkeletalMesh?>("WeaponMeshOffhandOverride");
        exportWeapons.AddIfNotNull(offhandSkeletalMesh);

        if (skeletalMesh is null)
        {
            var staticMesh = weaponDefinition.GetOrDefault<UStaticMesh?>("PickupStaticMesh");
            exportWeapons.AddIfNotNull(staticMesh);
        }

        if (exportWeapons.Count > 0) return exportWeapons;
        
        if (weaponDefinition.TryGetValue(out FStructFallback componentContainer, "ComponentContainer"))
        {
            var components = componentContainer.Get<UObject[]>("Components");
            if (components.FirstOrDefault(component => component.ExportType.Equals("FortItemComponent_Pickup")) is { } pickupComponent)
            {
                var staticMesh = pickupComponent.GetOrDefault<UStaticMesh?>("PickupStaticMesh");
                exportWeapons.AddIfNotNull(staticMesh);
            }
        }

        if (exportWeapons.Count > 0) return exportWeapons;
        
        if (weaponDefinition.TryGetValue(out UBlueprintGeneratedClass weaponActorClass, "WeaponActorClass"))
        {
            var weaponActorData = weaponActorClass.ClassDefaultObject.Load()!;
            if (weaponActorData.TryGetValue(out UObject weaponMeshData, "WeaponMesh"))
            {
                var weaponMesh = weaponMeshData.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                exportWeapons.AddIfNotNull(weaponMesh);
            }

            if (weaponActorData.TryGetValue(out UObject leftWeaponMeshData, "LeftHandWeaponMesh"))
            {
                var leftWeaponMesh = leftWeaponMeshData.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                exportWeapons.AddIfNotNull(leftWeaponMesh);
            }
        }

        if (exportWeapons.Count > 0) return exportWeapons;
        
        exportWeapons.AddRangeIfNotNull(weaponDefinition.GetDataListItems<UObject>("PickupSkeletalMesh", "PickupStaticMesh"));

        return exportWeapons;
    }
    
    public List<ExportObject> LevelSaveRecord(ULevelSaveRecord levelSaveRecord)
    {
        var objects = new List<ExportObject>();
        foreach (var (index, templateRecord) in levelSaveRecord.TemplateRecords)
        {
            var actorBlueprint = templateRecord.ActorClass.Load<UBlueprintGeneratedClass>();
            if (actorBlueprint is null) continue;
            
            objects.AddRangeIfNotNull(Blueprint(actorBlueprint));
            
            if (objects.Count == 0) continue;

            // todo proper byte actor data parsing
            var textureDatas = new Dictionary<int, UBuildingTextureData>();
            var actorData = levelSaveRecord.ActorData[index];
            if (actorData.TryGetAllValues(out string?[] textureDataRawPaths, "TextureData"))
            {
                for (var i = 0; i < textureDataRawPaths.Length; i++)
                {
                    var textureDataPath = textureDataRawPaths[i];
                    if (textureDataPath is null || string.IsNullOrEmpty(textureDataPath)) continue;
                    if (!CUE4ParseVM.Provider.TryLoadObject(textureDataPath, out UBuildingTextureData textureData)) continue;
                    textureDatas.Add(i, textureData);
                }
            }
            else
            {
                var textureDataPaths = templateRecord.ActorDataReferenceTable;
                for (var i = 0; i < textureDataPaths.Length; i++)
                {
                    var textureDataPath = textureDataPaths[i];
                    if (textureDataPath.AssetPathName.IsNone || string.IsNullOrEmpty(textureDataPath.AssetPathName.Text)) continue;
                    var textureData = textureDataPath.Load<UBuildingTextureData>();
                    textureDatas.Add(i, textureData);
                }
            }
            
            var targetMesh = objects.OfType<ExportMesh>().FirstOrDefault();
            foreach (var (textureDataIndex, textureData) in textureDatas)
            {
                targetMesh?.TextureData.AddIfNotNull(TextureData(textureData, textureDataIndex));
            }
        }

        return objects;
    }
     
    public ExportTextureData? TextureData(UBuildingTextureData? textureData, int index = 0)
    {
        if (textureData is null) return null;
        
        var exportTextureData = new ExportTextureData();

        var textureSuffix = index > 0 ? $"_Texture_{index + 1}" : string.Empty;
        var specSuffix = index > 0 ? $"_{index + 1}" : string.Empty;
        exportTextureData.Diffuse = AddData(textureData.Diffuse, "Diffuse", textureSuffix);
        exportTextureData.Normal = AddData(textureData.Normal, "Normals", textureSuffix);
        exportTextureData.Specular = AddData(textureData.Specular, "SpecularMasks", specSuffix);
        exportTextureData.Hash = textureData.GetPathName().GetHashCode();
        return exportTextureData;
        
        TextureParameter? AddData(UTexture? texture, string prefix, string suffix)
        {
            return texture is null ? default : new TextureParameter(prefix + suffix, Export(texture), texture.SRGB, texture.CompressionSettings);
        }
    }

    public List<ExportMesh> ExtraActorMeshes(UObject actor)
    {
        var extraMeshes = new List<ExportMesh>();
        if (actor.TryGetValue(out UStaticMesh doorMesh, "DoorMesh"))
        {
            var doorOffset = actor.GetOrDefault<TIntVector3<float>>("DoorOffset").ToFVector();
            var doorRotation = actor.GetOrDefault("DoorRotationOffset", FRotator.ZeroRotator);
            doorRotation.Pitch *= -1;
                
            var exportDoorMesh = Mesh(doorMesh)!;
            exportDoorMesh.Location = doorOffset;
            exportDoorMesh.Rotation = doorRotation;
            extraMeshes.AddIfNotNull(exportDoorMesh);

            if (actor.GetOrDefault("bDoubleDoor", false))
            {
                var exportDoubleDoorMesh = exportDoorMesh with
                {
                    Location = exportDoorMesh.Location with { X = -exportDoorMesh.Location.X },
                    Scale = exportDoorMesh.Scale with { X = -exportDoorMesh.Scale.X }
                };
                extraMeshes.AddIfNotNull(exportDoubleDoorMesh);
            }
            else if (actor.TryGetValue(out UStaticMesh doubleDoorMesh, "DoubleDoorMesh"))
            {
                var exportDoubleDoorMesh = Mesh(doubleDoorMesh)!;
                exportDoubleDoorMesh.Location = doorOffset;
                exportDoubleDoorMesh.Rotation = doorRotation;
                extraMeshes.AddIfNotNull(exportDoubleDoorMesh);
            }
                
        }

        return extraMeshes;
    }

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
        
        if (Meta.WorldFlags.HasFlag(EWorldFlags.WorldPartitionGrids) && level.GetOrDefault<UObject>("WorldSettings") is { } worldSettings
            && worldSettings.GetOrDefault<UObject>("WorldPartition") is { } worldPartition
            && worldPartition.GetOrDefault<UObject>("RuntimeHash") is { } runtimeHash)
        {
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
            if (actor.TryGetValue(out FPackageIndex[] instanceComponents, "InstanceComponents"))
            {
                foreach (var instanceComponentLazy in instanceComponents)
                {
                    var instanceComponent = instanceComponentLazy.Load<UInstancedStaticMeshComponent>();
                    if (instanceComponent is null) continue;
                    if (instanceComponent.ExportType == "HLODInstancedStaticMeshComponent") continue;

                    if (!Meta.WorldFlags.HasFlag(EWorldFlags.InstancedFoliage)) continue;
                    
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
                    var templateBlueprintGeneratedClass = CUE4ParseVM.Provider.LoadObject<UObject>(blueprintPath);
                    
                    exportMesh.AddChildren(ConstructionScript(templateBlueprintGeneratedClass));
                    exportMesh.AddChildren(InheritableComponentHandler(templateBlueprintGeneratedClass));
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
                    var templateBlueprintGeneratedClass = CUE4ParseVM.Provider.LoadObject<UObject>(blueprintPath);
                    
                    exportMesh.AddChildren(ConstructionScript(templateBlueprintGeneratedClass));
                    exportMesh.AddChildren(InheritableComponentHandler(templateBlueprintGeneratedClass));
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

        if (Meta.WorldFlags.HasFlag(EWorldFlags.Landscape) && actor is ALandscapeProxy landscapeProxy)
        {
            var transform = landscapeProxy.GetAbsoluteTransformFromRootComponent();
            
            var exportMesh = new ExportMesh();
            exportMesh.Name = landscapeProxy.Name;
            exportMesh.Path = Export(landscapeProxy, embeddedAsset: true, synchronousExport: true);
            exportMesh.Location = transform.Translation;
            exportMesh.Scale = transform.Scale3D;
            meshes.Add(exportMesh);
        }

        return meshes;
    }

    public List<ExportObject> Blueprint(UBlueprintGeneratedClass blueprintGeneratedClass)
    {
        var objects = new List<ExportObject>();
        
        objects.AddRangeIfNotNull(ConstructionScript(blueprintGeneratedClass));
        objects.AddRangeIfNotNull(InheritableComponentHandler(blueprintGeneratedClass));

        if (blueprintGeneratedClass.ClassDefaultObject.TryLoad(out var classDefaultObject))
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

    public ExportMesh? MeshComponent(UObject genericComponent)
    {
        return genericComponent switch
        {
            UInstancedStaticMeshComponent instancedStaticMeshComponent => MeshComponent(instancedStaticMeshComponent),
            UStaticMeshComponent staticMeshComponent => MeshComponent(staticMeshComponent),
            USkeletalMeshComponent skeletalMeshComponent => MeshComponent(skeletalMeshComponent),
            _ => null
        };
    }
    
    public ExportMesh? MeshComponent(USkeletalMeshComponent meshComponent)
    {
        var mesh = meshComponent.GetSkeletalMesh().Load<USkeletalMesh>();
        if (mesh is null) return null;

        var exportMesh = Mesh(mesh);
        if (exportMesh is null) return null;
        
        var overrideMaterials = meshComponent.GetOrDefault("OverrideMaterials", Array.Empty<UMaterialInterface?>());
        for (var idx = 0; idx < overrideMaterials.Length; idx++)
        {
            var material = overrideMaterials[idx];
            if (material is null) continue;

            exportMesh.OverrideMaterials.AddIfNotNull(Material(material, idx));
        }

        return exportMesh;
    }
    
    public ExportMesh? MeshComponent(UStaticMeshComponent meshComponent)
    {
        var mesh = meshComponent.GetStaticMesh().Load<UStaticMesh>();
        if (mesh is null) return null;

        var exportMesh = Mesh(mesh);
        if (exportMesh is null) return null;
        
        var overrideMaterials = meshComponent.GetOrDefault("OverrideMaterials", Array.Empty<UMaterialInterface?>());
        for (var idx = 0; idx < overrideMaterials.Length; idx++)
        {
            var material = overrideMaterials[idx];
            if (material is null) continue;
            
            exportMesh.OverrideMaterials.AddIfNotNull(Material(material, idx));
        }

        if (meshComponent.LODData?.FirstOrDefault()?.OverrideVertexColors is { } overrideVertexColors)
        {
            exportMesh.OverrideVertexColors = overrideVertexColors.Data;
        }

        return exportMesh;
    }

    public ExportMesh? MeshComponent(UInstancedStaticMeshComponent instanceComponent)
    {
        var mesh = instanceComponent.GetOrDefault<UStaticMesh?>("StaticMesh");
        var exportMesh = Mesh(mesh);
        if (exportMesh is null) return null;
                
        foreach (var instance in instanceComponent.PerInstanceSMData ?? [])
        {
            exportMesh.Instances.Add(new ExportTransform(instance.TransformData));
        }

        return exportMesh;
    }

    public ExportMesh? Mesh(UObject obj)
    {
        return obj switch
        {
            USkeletalMesh skeletalMesh => Mesh(skeletalMesh),
            UStaticMesh staticMesh => Mesh(staticMesh),
            USkeleton skeleton => Skeleton(skeleton),
            _ => null
        };
    }
    
    public ExportMesh? Mesh(USkeletalMesh? mesh)
    {
        return Mesh<ExportMesh>(mesh);
    }
    
    public T? Mesh<T>(USkeletalMesh? mesh) where T : ExportMesh, new()
    {
        if (mesh is null) return null;
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportPart = new T
        {
            Name = mesh.Name,
            Path = Export(mesh),
            NumLods = convertedMesh.LODs.Count
        };

        var sections = convertedMesh.LODs[0].Sections.Value;
        foreach (var (index, section) in sections.Enumerate())
        {
            if (section.Material is null) continue;
            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            exportPart.Materials.AddIfNotNull(Material(material, index));
        }

        return exportPart;
    }
    
    public ExportMesh? Mesh(UStaticMesh? mesh)
    {
        return Mesh<ExportMesh>(mesh);
    }
    
    public T? Mesh<T>(UStaticMesh? mesh) where T : ExportMesh, new()
    {
        if (mesh is null) return null;
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportPart = new T
        {
            Name = mesh.Name,
            Path = Export(mesh),
            NumLods = convertedMesh.LODs.Count
        };

        var sections = convertedMesh.LODs[0].Sections.Value;
        foreach (var (index, section) in sections.Enumerate())
        {
            if (section.Material is null) continue;
            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            exportPart.Materials.AddIfNotNull(Material(material, index));
        }

        return exportPart;
    }
    
    public ExportMesh? Skeleton(USkeleton? skeleton)
    {
        if (skeleton is null) return null;

        var exportMesh = new ExportMesh
        {
            Name = skeleton.Name,
            Path = Export(skeleton)
        };

        return exportMesh;
    }

    
    public ExportAnimSection? AnimSequence(UAnimSequence? animSequence, float time = 0.0f)
    {
        if (animSequence is null) return null;
        var exportSequence = new ExportAnimSection
        {
            Path = Export(animSequence),
            Name = animSequence.Name,
            Length = animSequence.SequenceLength,
            Time = time
        };
        
        var floatCurves = animSequence.CompressedCurveData.FloatCurves ?? [];
        foreach (var curve in floatCurves)
        {
            exportSequence.Curves.Add(new ExportCurve
            {
                Name = curve.CurveName.Text,
                Keys = curve.FloatCurve.Keys.Select(x => new ExportCurveKey(x.Time, x.Value)).ToList()
            });
        }

        return exportSequence;
    }
    
    public ExportAnimSection? AnimSequence(UAnimSequence? additiveSequence, UAnimSequence? baseSequence, float time = 0.0f)
    {
        if (additiveSequence is null) return null;
        if (baseSequence is null) return null;
        
        additiveSequence.RefPoseSeq = new ResolvedLoadedObject(baseSequence);

        var exportSequence = new ExportAnimSection
        {
            Path = Export(additiveSequence),
            Name = additiveSequence.Name,
            Length = additiveSequence.SequenceLength,
            Time = time
        };
        
        var baseFloatCurves = baseSequence.CompressedCurveData.FloatCurves ?? [];
        var additiveFloatCurves = additiveSequence.CompressedCurveData.FloatCurves ?? [];

        FFloatCurve[] floatCurves = [..baseFloatCurves, ..additiveFloatCurves];
        foreach (var curve in floatCurves)
        {
            exportSequence.Curves.Add(new ExportCurve
            {
                Name = curve.CurveName.Text,
                Keys = curve.FloatCurve.Keys.Select(x => new ExportCurveKey(x.Time, x.Value)).ToList()
            });
        }

        return exportSequence;
    }

    public ExportMaterial? Material(UMaterialInterface material, int index)
    {
        if (!Meta.Settings.ExportMaterials) return null;

        var hash = material.GetPathName().GetHashCode();
        if (MaterialCache.FirstOrDefault(mat => mat.Hash == hash) is { } existing) return existing with { Slot = index};

        var exportMaterial = new ExportMaterial
        {
            Path = material.GetPathName(),
            Name = material.Name,
            Slot = index,
            Hash = hash
        };

        AccumulateParameters(material, ref exportMaterial);

        exportMaterial.OverrideBlendMode = (material as UMaterialInstanceConstant)?.BasePropertyOverrides?.BlendMode ?? exportMaterial.BaseBlendMode;

        MaterialCache.Add(exportMaterial);
        return exportMaterial;
    }
    
    public ExportMaterial? OverrideMaterial(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var material = Material(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        return material;
    }
    
    public ExportOverrideMaterial? OverrideMaterialSwap(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var exportMaterial = Material(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        if (exportMaterial is null) return null;

        return new ExportOverrideMaterial
        {
            Material = exportMaterial,
            MaterialNameToSwap = overrideData.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.Text.SubstringAfterLast(".")
        };
    }
    
    public ExportOverrideParameters? OverrideParameters(FStructFallback overrideData)
    {
        var materialToAlter = overrideData.Get<FSoftObjectPath>("MaterialToAlter");
        if (materialToAlter.AssetPathName.IsNone) return null; 

        var exportParams = new ExportOverrideParameters();
        AccumulateParameters(overrideData, ref exportParams);
        
        exportParams.MaterialNameToAlter = materialToAlter.AssetPathName.Text.SubstringAfterLast(".");
        exportParams.Hash = exportParams.GetHashCode();
        return exportParams;
    }
    
    public void AccumulateParameters<T>(UMaterialInterface? materialInterface, ref T parameterCollection) where T : ParameterCollection
    {
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            foreach (var param in materialInstance.TextureParameterValues)
            {
                if (parameterCollection.Textures.Any(x => x.Name.Equals(param.Name))) continue;
                if (!param.ParameterValue.TryLoad(out UTexture texture)) continue;
                parameterCollection.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
            }

            foreach (var param in materialInstance.ScalarParameterValues)
            {
                if (parameterCollection.Scalars.Any(x => x.Name.Equals(param.Name))) continue;
                parameterCollection.Scalars.AddUnique(new ScalarParameter(param.Name, param.ParameterValue));
            }

            foreach (var param in materialInstance.VectorParameterValues)
            {
                if (parameterCollection.Vectors.Any(x => x.Name.Equals(param.Name))) continue;
                if (param.ParameterValue is null) continue;
                parameterCollection.Vectors.AddUnique(new VectorParameter(param.Name, param.ParameterValue.Value));
            }

            if (materialInstance.StaticParameters is not null)
            {
                foreach (var param in materialInstance.StaticParameters.StaticSwitchParameters)
                {
                    if (parameterCollection.Switches.Any(x => x.Name.Equals(param.Name))) continue;
                    parameterCollection.Switches.AddUnique(new SwitchParameter(param.Name, param.Value));
                }

                foreach (var param in materialInstance.StaticParameters.StaticComponentMaskParameters)
                {
                    if (parameterCollection.ComponentMasks.Any(x => x.Name.Equals(param.Name))) continue;

                    parameterCollection.ComponentMasks.AddUnique(new ComponentMaskParameter(param.Name, param.ToLinearColor()));
                }
            }
            
            if (materialInstance.TryLoadEditorData<UMaterialInstanceEditorOnlyData>(out var materialInstanceEditorData) && materialInstanceEditorData?.StaticParameters is not null)
            {
                foreach (var parameter in materialInstanceEditorData.StaticParameters.StaticSwitchParameters)
                {
                    if (parameter.ParameterInfo is null) continue;
                    parameterCollection.Switches.AddUnique(new SwitchParameter(parameter.Name, parameter.Value));
                }

                foreach (var parameter in materialInstanceEditorData.StaticParameters.StaticComponentMaskParameters)
                {
                    if (parameter.ParameterInfo is null) continue;
                    parameterCollection.ComponentMasks.AddUnique(new ComponentMaskParameter(parameter.Name, parameter.ToLinearColor()));
                }
            }

            if (materialInstance.Parent is UMaterialInterface parentMaterial) AccumulateParameters(parentMaterial, ref parameterCollection);
        }
        else if (materialInterface is UMaterial material)
        {
            if (parameterCollection is ExportMaterial exportMaterial)
            {
                exportMaterial.BaseMaterial = material;
                exportMaterial.PhysMaterialName =
                    material.GetOrDefault<FPackageIndex?>("PhysMaterial")?.Name ?? string.Empty;
            }

            if (parameterCollection.Textures.Count == 0 && !material.Name.Contains("Parent", StringComparison.OrdinalIgnoreCase))
            {
                AccumulateParameters(material, ref parameterCollection);
            }
        }
    }
    
    public void AccumulateParameters<T>(UMaterial material, ref T parameterCollection) where T : ParameterCollection
    {
        // TODO use uefn data and custom FPackageIndex resolver to start reading material tree 
        var parameters = new CMaterialParams2();
        material.GetParams(parameters, EMaterialFormat.AllLayers);
                
        foreach (var (name, value) in parameters.Textures)
        {
            if (value is not UTexture2D texture) continue;
            
            parameterCollection.Textures.AddUnique(new TextureParameter(name, Export(texture), texture.SRGB, texture.CompressionSettings));
        }
    }
    
    public void AccumulateParameters<T>(FStructFallback data, ref T parameterCollection) where T : ParameterCollection
    {
        var textureParams = data.GetOrDefault<FStyleParameter<FSoftObjectPath>[]>("TextureParams");
        foreach (var param in textureParams)
        {
            if (parameterCollection.Textures.Any(x => x.Name == param.Name)) continue;
            if (!param.Value.TryLoad(out UTexture texture)) continue;
            parameterCollection.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
        }

        var floatParams = data.GetOrDefault<FStyleParameter<float>[]>("FloatParams");
        foreach (var param in floatParams)
        {
            if (parameterCollection.Scalars.Any(x => x.Name == param.Name)) continue;
            parameterCollection.Scalars.AddUnique(new ScalarParameter(param.Name, param.Value));
        }

        var colorParams = data.GetOrDefault<FStyleParameter<FLinearColor>[]>("ColorParams");
        foreach (var param in colorParams)
        {
            if (parameterCollection.Vectors.Any(x => x.Name == param.ParamName.Text)) continue;
            parameterCollection.Vectors.AddUnique(new VectorParameter(param.Name, param.Value));
        }
    }

    public async Task<string> ExportAsync(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false)
    {
        var extension = asset switch
        {
            USkeletalMesh or UStaticMesh or USkeleton => Meta.Settings.MeshFormat switch
            {
                EMeshFormat.UEFormat => "uemodel",
                EMeshFormat.ActorX => "psk",
                EMeshFormat.Gltf2 => "glb",
                EMeshFormat.OBJ => "obj",
            },
            UAnimSequence => Meta.Settings.AnimFormat switch
            {
                EAnimFormat.UEFormat => "ueanim",
                EAnimFormat.ActorX => "psa"
            },
            UTexture => Meta.Settings.ImageFormat switch
            {
                EImageFormat.PNG => "png",
                EImageFormat.TGA => "tga"
            },
            USoundWave => Meta.Settings.SoundFormat switch
            {
                ESoundFormat.WAV => "wav",
                ESoundFormat.MP3 => "mp3",
                ESoundFormat.OGG => "ogg",
                ESoundFormat.FLAC => "flac",
            },
            ALandscapeProxy => "uemodel",
            UFontFace => "ttf"
        };

        var path = GetExportPath(asset, extension, embeddedAsset, excludeGamePath: Meta.CustomPath is not null);
        
        var returnValue = returnRealPath ? path : (embeddedAsset ? $"{asset.Owner.Name}/{asset.Name}.{asset.Name}" : asset.GetPathName());

        var shouldExport = asset switch
        {
            UTexture texture => IsTextureHigherResolutionThanExisting(texture, path),
            ALandscapeProxy => true,
            _ => !File.Exists(path)
        };

        if (!shouldExport) return returnValue;

        var exportTask = new Task(() =>
        {
            try
            {
                Log.Information("Exporting {ExportType}: {Path}", asset.ExportType, path);
                Export(asset, path);
            }
            catch (IOException e)
            {
                if ((e.HResult & 0x0000FFFF) == 32) return; // locked files, move on, it's being exported anyways
                
                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.ToString());
            }
        });
        
        ExportTasks.Add(exportTask);

        if (synchronousExport)
            exportTask.RunSynchronously();
        else
            exportTask.RunAsynchronously();

        return returnValue;
    }
    
    
    public string Export(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false)
    {
        return ExportAsync(asset, returnRealPath, synchronousExport, embeddedAsset).GetAwaiter().GetResult();
    }

    private void Export(UObject asset, string path)
    {
        switch (asset)
        {
            case USkeletalMesh skeletalMesh:
            {
                var exporter = new MeshExporter(skeletalMesh, FileExportOptions);
                foreach (var mesh in exporter.MeshLods)
                {
                    File.WriteAllBytes(path, mesh.FileData);
                }
                break;
            }
            case UStaticMesh staticMesh:
            {
                var exporter = new MeshExporter(staticMesh, FileExportOptions);
                foreach (var mesh in exporter.MeshLods)
                {
                    File.WriteAllBytes(path, mesh.FileData);
                }
                break;
            }
            case USkeleton skeleton:
            {
                var exporter = new MeshExporter(skeleton, FileExportOptions);
                foreach (var skel in exporter.MeshLods)
                {
                    File.WriteAllBytes(path, skel.FileData);
                }
                break;
            }
            case UAnimSequence animSequence:
            {
                var exporter = new AnimExporter(animSequence, FileExportOptions);
                foreach (var sequence in exporter.AnimSequences)
                {
                    File.WriteAllBytes(path, sequence.FileData);
                }
                break;
            }
            case UTexture texture:
            {
                if (texture is UTexture2DArray && texture.GetFirstMip() is { } mip)
                {
                    for (var layerIndex = 0; layerIndex < mip.SizeZ; layerIndex++)
                    {
                        var textureBitmap = texture.Decode(mip, zLayer: layerIndex);
                        var texturePath = path.Replace(".png", $"_{layerIndex}.png");
                        ExportBitmap(textureBitmap, texturePath);
                    }
                }
                else
                {
                    var textureBitmap = texture.Decode();
                    if (texture is UTextureCube) textureBitmap = textureBitmap.ToPanorama();
                    
                    ExportBitmap(textureBitmap, path);
                }

                break;
            }
            case USoundWave soundWave:
            {
                var wavPath = Path.ChangeExtension(path, "wav");
                if (!SoundExtensions.TrySaveSoundToPath(soundWave, wavPath))
                {
                    throw new Exception($"Failed to export sound '{soundWave.Name}' at {path}");
                }

                if (Meta.Settings.SoundFormat is not ESoundFormat.WAV)
                {
                    var extension = Path.GetExtension(path)[1..];
                    
                    // convert to format
                    FFMpegArguments.FromFileInput(wavPath)
                        .OutputToFile(path, true, options => options.ForceFormat(extension))
                        .ProcessSynchronously();
                        
                    File.Delete(wavPath); // delete old wav
                }

                
                break;
            }
            case ALandscapeProxy landscapeProxy:
            {
                var processor = new LandscapeProcessor(landscapeProxy);
                var mesh = processor.Process();

                var archive = new FArchiveWriter();
                var model = new UEModel(landscapeProxy.Name, mesh, new FPackageIndex(), FileExportOptions);
                model.Save(archive);

                File.WriteAllBytes(path, archive.GetBuffer());
                break;
            }
            case UFontFace fontFace:
            {
                if (!CUE4ParseVM.Provider.TrySavePackage(fontFace.GetPathName().SubstringBeforeLast(".") + ".ufont",
                        out var assets) || assets.Count == 0) break;

                var fontData = assets.First().Value;
                File.WriteAllBytes(path, fontData);
                break;
            }
        }
    }

    private bool IsTextureHigherResolutionThanExisting(UTexture texture, string path)
    {
        try
        {
            if (!File.Exists(path)) return true;
            
            using var file = File.OpenRead(path);
            using var image = Image.FromStream(file, useEmbeddedColorManagement: false, validateImageData: false);
            
            var mip = texture.GetFirstMip();
            if (mip is null) return true;
            
            return mip.SizeX > image.PhysicalDimension.Width && mip?.SizeY > image.PhysicalDimension.Height;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private void ExportBitmap(SKBitmap? bitmap, string path)
    {
        using var fileStream = File.OpenWrite(path); 
                
        var format = Meta.Settings.ImageFormat switch
        {
            EImageFormat.PNG => ETextureFormat.Png,
            EImageFormat.TGA => ETextureFormat.Tga
        };
                
        bitmap?.Encode(format, 100).SaveTo(fileStream); 
    }
    
    public string GetExportPath(UObject obj, string ext, bool embeddedAsset = false, bool excludeGamePath = false)
    {
        string path;
        if (excludeGamePath || obj.Owner is null)
        {
            path = obj.Name;
        }
        else
        {
            path = embeddedAsset ? $"{obj.Owner.Name}/{obj.Name}" : obj.Owner?.Name ?? string.Empty;
        }
        
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(Meta.CustomPath ?? Meta.AssetsRoot, path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        var finalPath = $"{directory}.{ext.ToLower()}";
        return finalPath;
    }
}