using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using Serilog;

namespace FortnitePorting.Export.Types;

public class MeshExportData : ExportDataBase
{
    public readonly List<ExportMesh> Meshes = new();
    public readonly List<ExportMesh> OverrideMeshes = new();
    public readonly List<ExportOverrideMaterial> OverrideMaterials = new();
    public readonly List<ExportOverrideParameters> OverrideParameters = new();

    public MeshExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportTargetType exportType) : base(name, asset, styles, type, EExportType.Mesh, exportType)
    {
        switch (type)
        {
            case EAssetType.Outfit:
            {
                var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                AssetsVM.ExportChunks = parts.Length;
                foreach (var part in parts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                    AssetsVM.ExportProgress++;
                }

                break;
            }
            case EAssetType.LegoOutfit:
            {
                throw new Exception("Lego Outfit export not supported yet.");
            }
            case EAssetType.Backpack:
            {
                var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                foreach (var part in parts) Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                break;
            }
            case EAssetType.Pickaxe:
            {
                var weapon = asset.GetOrDefault<UObject?>("WeaponDefinition");
                if (weapon is null) break;

                Meshes.AddRange(Exporter.WeaponDefinition(weapon));
                break;
            }
            case EAssetType.Glider:
            {
                var mesh = asset.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                if (mesh is null) break;

                var part = Exporter.Mesh(mesh);
                if (part is null) break;

                var overrideMaterials = asset.GetOrDefault("MaterialOverrides", Array.Empty<FStructFallback>());
                foreach (var overrideMaterial in overrideMaterials) part.OverrideMaterials.AddIfNotNull(Exporter.OverrideMaterial(overrideMaterial));

                Meshes.Add(part);
                break;
            }
            case EAssetType.Pet:
            {
                // backpack meshes
                var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                foreach (var part in parts) Meshes.AddIfNotNull(Exporter.CharacterPart(part));

                // pet mesh
                var petAsset = asset.Get<UObject>("DefaultPet");
                var blueprintPath = petAsset.Get<FSoftObjectPath>("PetPrefabClass");
                var blueprintExports = CUE4ParseVM.Provider.LoadAllObjects(blueprintPath.AssetPathName.Text.SubstringBeforeLast("."));
                var meshComponent = blueprintExports.FirstOrDefault(export => export.Name.Equals("PetMesh0")) as USkeletalMeshComponentBudgeted;
                var mesh = meshComponent?.GetSkeletalMesh().Load<USkeletalMesh>();
                if (mesh is not null) Meshes.AddIfNotNull(Exporter.Mesh(mesh));

                break;
            }
            case EAssetType.Toy:
            {
                var actor = asset.Get<UBlueprintGeneratedClass>("ToyActorClass");
                var parentActor = actor.SuperStruct.Load<UBlueprintGeneratedClass>();

                var exportComponent = GetComponent(actor);
                exportComponent ??= GetComponent(parentActor);
                if (exportComponent is null) break;

                Meshes.AddIfNotNull(Exporter.MeshComponent(exportComponent));
                break;

                UStaticMeshComponent? GetComponent(UBlueprintGeneratedClass? blueprint)
                {
                    if (blueprint is null) return null;
                    if (!blueprint.TryGetValue(out UObject internalComponentHandler, "InheritableComponentHandler")) return null;

                    var records = internalComponentHandler.GetOrDefault("Records", Array.Empty<FStructFallback>());
                    foreach (var record in records)
                    {
                        var component = record.Get<UObject>("ComponentTemplate");
                        if (component is not UStaticMeshComponent staticMeshComponent) continue;

                        return staticMeshComponent;
                    }

                    return null;
                }
            }
            case EAssetType.Prop:
            {
                var levelSaveRecord = asset.Get<ULevelSaveRecord>("ActorSaveRecord");
                var meshes = Exporter.LevelSaveRecord(levelSaveRecord);
                Meshes.AddRange(meshes);
                break;
            }
            case EAssetType.Prefab:
            {
                if (asset.TryGetValue(out ULevelSaveRecord baseSaveRecord, "LevelSaveRecord"))
                {
                    Meshes.AddRange(Exporter.LevelSaveRecord(baseSaveRecord));
                }
                
                var recordCollectionLazy = asset.GetOrDefault<FPackageIndex?>("PlaysetPropLevelSaveRecordCollection");
                if (recordCollectionLazy is null || recordCollectionLazy.IsNull || !recordCollectionLazy.TryLoad(out var recordCollection) || recordCollection is null) break;

                var props = recordCollection.GetOrDefault<FStructFallback[]>("Items");
                AssetsVM.ExportChunks = props.Length;
                AssetsVM.ExportProgress = 0;
                foreach (var prop in props)
                {
                    if (AssetsVM.ExportProgress % 100 == 0) GC.Collect();
                    var levelSaveRecord = prop.GetOrDefault<UObject?>("LevelSaveRecord");
                    if (levelSaveRecord is null) continue;

                    var actorSaveRecord = levelSaveRecord.Get<ULevelSaveRecord>("ActorSaveRecord");
                    var transform = prop.GetOrDefault<FTransform>("Transform");
                    var meshes = Exporter.LevelSaveRecord(actorSaveRecord);
                    foreach (var mesh in meshes)
                    {
                        mesh.Location += transform.Translation;
                        mesh.Rotation += transform.Rotator();
                        mesh.Scale *= transform.Scale3D;
                    }

                    Meshes.AddRange(meshes);
                    AssetsVM.ExportProgress++;
                }

                break;
            }
            case EAssetType.Item:
            {
                Meshes.AddRange(Exporter.WeaponDefinition(asset));
                break;
            }
            case EAssetType.Resource:
            {
                Meshes.AddRange(Exporter.WeaponDefinition(asset));
                break;
            }
            case EAssetType.Trap:
            {
                var actor = asset.Get<UBlueprintGeneratedClass>("BlueprintClass").ClassDefaultObject.Load();
                if (actor is null) break;

                var staticMesh = actor.GetOrDefault<UBaseBuildingStaticMeshComponent?>("StaticMeshComponent");
                if (staticMesh is not null)
                {
                    Meshes.AddIfNotNull(Exporter.MeshComponent(staticMesh));
                }

                var components = CUE4ParseVM.Provider.LoadAllObjects(actor.GetPathName().SubstringBeforeLast("."));
                foreach (var component in components)
                {
                    if (component.Name.Equals(staticMesh?.Name)) continue;
                    Meshes.AddIfNotNull(component switch
                    {
                        UStaticMeshComponent staticMeshComponent => Exporter.MeshComponent(staticMeshComponent),
                        USkeletalMeshComponent skeletalMeshComponent => Exporter.MeshComponent(skeletalMeshComponent),
                        _ => null
                    });
                }

                break;
            }
            case EAssetType.Vehicle:
            {
                var actor = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass").ClassDefaultObject.Load();
                if (actor is null) break;

                var skeletalMesh = actor.GetOrDefault<UFortVehicleSkelMeshComponent?>("SkeletalMesh");
                if (skeletalMesh is not null)
                {
                    Meshes.AddIfNotNull(Exporter.MeshComponent(skeletalMesh));
                }

                var components = CUE4ParseVM.Provider.LoadAllObjects(actor.GetPathName().SubstringBeforeLast("."));
                foreach (var component in components)
                {
                    if (component.Name.Equals(skeletalMesh?.Name)) continue;
                    Meshes.AddIfNotNull(component switch
                    {
                        UStaticMeshComponent staticMeshComponent => Exporter.MeshComponent(staticMeshComponent),
                        _ => null
                    });
                }

                break;
            }
            case EAssetType.Wildlife:
            {
                var wildlifeMesh = (USkeletalMesh) asset;
                Meshes.AddIfNotNull(Exporter.Mesh(wildlifeMesh));
                break;
            }
            case EAssetType.WeaponMod:
            {
                var mesh = asset.GetOrDefault<UStaticMesh?>("PickupStaticMesh");
                Meshes.AddIfNotNull(Exporter.Mesh(mesh));
                break;
            }
            case EAssetType.Mesh:
            {
                switch (asset)
                {
                    case USkeletalMesh skeletalMesh:
                        Meshes.AddIfNotNull(Exporter.Mesh(skeletalMesh));
                        break;
                    case UStaticMesh staticMesh:
                        Meshes.AddIfNotNull(Exporter.Mesh(staticMesh));
                        break;
                }

                break;
            }
            case EAssetType.World:
            {
                if (asset is not UWorld world) break;
                if (world.PersistentLevel.Load() is not ULevel level) break;

                FilesVM.ExportChunks = level.Actors.Length;
                AssetsVM.ExportProgress = 0;
                foreach (var actorLazy in level.Actors)
                {
                    FilesVM.ExportProgress++;
                    if (FilesVM.ExportProgress % 100 == 0) GC.Collect();
                    if (actorLazy is null || actorLazy.IsNull) continue;

                    var actor = actorLazy.Load();
                    if (actor is null) continue;
                    if (actor.ExportType == "LODActor") continue;
                    if (actor.Name.StartsWith("LF_")) continue;

                    Log.Information("Processing {0}: {1}/{2}", actor.Name, FilesVM.ExportProgress, FilesVM.ExportChunks);
                    ProcessActor(actor);
                }

                break;

                void ProcessActor(UObject actor)
                {
                    if (actor.TryGetValue(out UStaticMeshComponent staticMeshComponent, "StaticMeshComponent", "StaticMesh", "Mesh", "LightMesh"))
                    {
                        var exportMesh = Exporter.MeshComponent(staticMeshComponent);
                        if (exportMesh is null) return;

                        exportMesh.Name = actor.Name;
                        exportMesh.Location = staticMeshComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector);
                        exportMesh.Rotation = staticMeshComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator);
                        exportMesh.Scale = staticMeshComponent.GetOrDefault("RelativeScale3D", FVector.OneVector);

                        foreach (var extraMesh in Exporter.ExtraActorMeshes(actor))
                        {
                            exportMesh.Children.AddIfNotNull(extraMesh);
                        }

                        var textureDatas = actor.GetAllProperties<UBuildingTextureData>("TextureData");
                        if (textureDatas.Count == 0 && actor.Template is not null)
                            textureDatas = actor.Template.Load()!.GetAllProperties<UBuildingTextureData>("TextureData");

                        foreach (var (textureData, index) in textureDatas)
                        {
                            exportMesh.TextureData.AddIfNotNull(Exporter.TextureData(textureData, index));
                        }

                        Meshes.AddIfNotNull(exportMesh);
                    }
                }
            }
            case EAssetType.FestivalBass:
            case EAssetType.FestivalDrum:
            case EAssetType.FestivalGuitar:
            case EAssetType.FestivalKeytar:
            case EAssetType.FestivalMic:
            {
                if (asset.TryGetValue(out USkeletalMesh mesh, "Mesh"))
                {
                    var exportMesh = Exporter.Mesh(mesh);

                    var material = asset.GetOrDefault<UMaterialInterface>("Material");
                    exportMesh?.Materials.AddIfNotNull(Exporter.Material(material, 0));

                    Meshes.AddIfNotNull(exportMesh);
                }

                if (asset.TryGetValue(out USkeletalMesh leftHandMesh, "LeftHandMesh"))
                {
                    var exportMesh = Exporter.Mesh(leftHandMesh);

                    var material = asset.GetOrDefault<UMaterialInterface>("LeftHandMaterial");
                    exportMesh?.Materials.AddIfNotNull(Exporter.Material(material, 0));

                    Meshes.AddIfNotNull(exportMesh);
                }

                if (asset.TryGetValue(out USkeletalMesh auxiliaryMesh, "AuxiliaryMesh"))
                {
                    var exportMesh = Exporter.Mesh(auxiliaryMesh);

                    var auxMaterial = asset.GetOrDefault<UMaterialInterface>("AuxiliaryMaterial");
                    exportMesh?.Materials.AddIfNotNull(Exporter.Material(auxMaterial, 0));

                    var auxMaterial2 = asset.GetOrDefault<UMaterialInterface>("AuxiliaryMaterial2");
                    exportMesh?.Materials.AddIfNotNull(Exporter.Material(auxMaterial2, 1));

                    Meshes.AddIfNotNull(exportMesh);
                }


                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        ExportStyles(asset, styles);
    }

    private void ExportStyles(UObject asset, FStructFallback[] styles)
    {
        var metaTagsToApply = new List<FGameplayTag>();
        var metaTagsToRemove = new List<FGameplayTag>();
        foreach (var style in styles)
        {
            var tags = style.Get<FStructFallback>("MetaTags");

            var tagsToApply = tags.Get<FGameplayTagContainer>("MetaTagsToApply");
            metaTagsToApply.AddRange(tagsToApply.GameplayTags);

            var tagsToRemove = tags.Get<FGameplayTagContainer>("MetaTagsToRemove");
            metaTagsToRemove.AddRange(tagsToRemove.GameplayTags);
        }

        var metaTags = new FGameplayTagContainer(metaTagsToApply.Where(tag => !metaTagsToRemove.Contains(tag)).ToArray());
        var itemStyles = asset.GetOrDefault("ItemVariants", Array.Empty<UObject>());
        var tagDrivenStyles = itemStyles.Where(style => style.ExportType.Equals("FortCosmeticLoadoutTagDrivenVariant"));
        foreach (var tagDrivenStyle in tagDrivenStyles)
        {
            var options = tagDrivenStyle.Get<FStructFallback[]>("Variants");
            foreach (var option in options)
            {
                var requiredConditions = option.Get<FStructFallback[]>("RequiredConditions");
                foreach (var condition in requiredConditions)
                {
                    var metaTagQuery = condition.Get<FGameplayTagQuery>("MetaTagQuery");
                    if (metaTags.MatchesQuery(metaTagQuery)) ExportStyleData(option);
                }
            }
        }

        foreach (var style in styles) ExportStyleData(style);
    }

    private void ExportStyleData(FStructFallback style)
    {
        var variantParts = style.GetOrDefault("VariantParts", Array.Empty<UObject>());
        foreach (var part in variantParts) OverrideMeshes.AddIfNotNull(Exporter.CharacterPart(part));

        var variantMaterials = style.GetOrDefault("VariantMaterials", Array.Empty<FStructFallback>());
        foreach (var material in variantMaterials) OverrideMaterials.AddIfNotNull(Exporter.OverrideMaterial(material));

        var variantParameters = style.GetOrDefault("VariantMaterialParams", Array.Empty<FStructFallback>());
        foreach (var parameters in variantParameters) OverrideParameters.AddIfNotNull(Exporter.OverrideParameters(parameters));
    }
}