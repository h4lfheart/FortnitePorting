using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
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
                        if (animBlueprint.ClassDefaultObject != null 
                            && animBlueprint.ClassDefaultObject.TryLoad(out var animBlueprintData) 
                            && animBlueprintData.TryGetValue(out FStructFallback poseAssetNode, "AnimGraphNode_PoseBlendNode"))
                        {
                            meta.PoseAsset = Export(poseAssetNode.Get<UPoseAsset>("PoseAsset"));
                        }
                        else if (skeletalMesh.ReferenceSkeleton.FinalRefBoneInfo.Any(bone => bone.Name.Text.Equals("FACIAL_C_FacialRoot", StringComparison.OrdinalIgnoreCase)))
                        {
                            var foundDNA = false;
                            foreach (var userData in skeletalMesh.AssetUserData)
                            {
                                if (!userData.TryLoad<UDNAAsset>(out var dna)) continue;
                                
                                meta.PoseAsset = Export(dna);
                                foundDNA = meta.PoseAsset != null; //TODO: how do we know this succeeded? Or should we just assume it did?
                                break;
                            }
                            // Fallback in case DNA exporting fails
                            if (!foundDNA && UEParse.Provider.TryLoadPackageObject("/BRCosmetics/Characters/Player/Male/Medium/Heads/M_MED_Jonesy3L_Head/Meshes/3L/3L_lod2_Facial_Poses_PoseAsset", out UPoseAsset poseAsset)) 
                                meta.PoseAsset = Export(poseAsset);
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
    
    public List<UObject> WeaponDefinitionMeshes(UObject weaponDefinition)
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
    
    public ExportTextureData? TextureData(UBuildingTextureData? textureData, int index = 0)
    {
        if (textureData is null) return null;
        
        var exportTextureData = new ExportTextureData();

        var textureSuffix = index > 0 ? $"_Texture_{index + 1}" : string.Empty;
        var specSuffix = index > 0 ? $"_{index + 1}" : string.Empty;
        
        exportTextureData.Diffuse = AddData(textureData.Diffuse, "Diffuse", textureSuffix);
        exportTextureData.Normal = AddData(textureData.Normal, "Normals", textureSuffix);
        exportTextureData.Specular = AddData(textureData.Specular, "SpecularMasks", specSuffix);
        
        if (textureData.OverrideMaterial is { } overrideMaterial)
            exportTextureData.OverrideMaterial = Material(overrideMaterial, 0);
        
        exportTextureData.Hash = textureData.GetPathName().GetHashCode();
        
        return exportTextureData;
        
        TextureParameter? AddData(UTexture? texture, string prefix, string suffix)
        {
            return texture is null ? null : new TextureParameter(prefix + suffix, Export(texture), texture.SRGB, texture.CompressionSettings);
        }
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
                    if (!UEParse.Provider.TryLoadPackageObject(textureDataPath, out UBuildingTextureData textureData)) continue;
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
    
    public List<ExportMesh> ExtraActorMeshes(UObject actor)
    {
        var extraMeshes = new List<ExportMesh>();
        if (actor.TryGetValue(out UStaticMesh doorMesh, "DoorMesh"))
        {
            var doorOffset = actor.GetOrDefault<TIntVector3<float>>("DoorOffset").ToFVector();
            var doorRotation = actor.GetOrDefault("DoorRotationOffset", FRotator.ZeroRotator);
            doorRotation.Pitch *= -1;
                
            var exportDoorMesh = Mesh(doorMesh);
            if (exportDoorMesh != null)
            {
                exportDoorMesh.Location = doorOffset;
                exportDoorMesh.Rotation = doorRotation;
                extraMeshes.AddIfNotNull(exportDoorMesh);
            }
            if (exportDoorMesh != null && actor.GetOrDefault("bDoubleDoor", false))
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
}