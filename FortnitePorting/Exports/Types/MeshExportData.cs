using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Exports.Types;

public class MeshExportData : ExportDataBase
{
    public List<ExportMesh> Parts = new();
    public List<ExportMesh> StyleParts = new();
    public List<ExportMaterial> StyleMaterials = new();
    public List<ExportMeshOverride> StyleMeshes = new();
    public List<ExportMaterialParams> StyleMaterialParams = new();
    public AnimationData? LinkedSequence;

    public static async Task<MeshExportData?> Create(UObject asset, EAssetType assetType, FStructFallback[] styles)
    {
        var data = new MeshExportData();
        data.Name = assetType is (EAssetType.Mesh or EAssetType.Wildlife) ? asset.Name : asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        data.Type = assetType.ToString();
        var canContinue = await Task.Run(async () =>
        {
            switch (assetType)
            {
                case EAssetType.Outfit:
                {
                    var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                    if (asset.TryGetValue(out UObject heroDefinition, "HeroDefinition"))
                    {
                        if (parts.Length == 0)
                        {
                            var specializations = heroDefinition.Get<UObject[]>("Specializations").FirstOrDefault();
                            parts = specializations?.GetOrDefault("CharacterParts", Array.Empty<UObject>()) ?? Array.Empty<UObject>();
                        }

                        var frontendAnimMontage = heroDefinition.GetOrDefault<UAnimMontage?>("FrontendAnimMontageIdleOverride");
                        if (frontendAnimMontage is not null && AppSettings.Current.BlenderExportSettings.LobbyPoses)
                        {
                            data.LinkedSequence = await DanceExportData.CreateAnimDataAsync(frontendAnimMontage);
                        }
                    }

                    var exportedParts = ExportHelpers.CharacterParts(parts, data.Parts);

                    if (asset.TryGetValue(out UObject[] characterParts, "BaseCharacterParts"))
                    {
                        foreach (var characterPart in characterParts)
                        {
                            var frontendAnimMontage = characterPart.GetOrDefault<UAnimMontage?>("FrontendAnimMontageIdleOverride");
                            if (frontendAnimMontage is not null && AppSettings.Current.BlenderExportSettings.LobbyPoses)
                            {
                                data.LinkedSequence = await DanceExportData.CreateAnimDataAsync(frontendAnimMontage);
                            }

                            break;
                        }
                    }

                    if (data.LinkedSequence is null && AppSettings.Current.BlenderExportSettings.LobbyPoses) // fallback
                    {
                        var bodyPart = exportedParts.FirstOrDefault(x => x.Part.Equals("Body"));
                        if (bodyPart is null) break;

                        var targetMontage = bodyPart.GenderPermitted switch
                        {
                            EFortCustomGender.Male => AppVM.CUE4ParseVM.MaleIdleAnimations.Random(),
                            EFortCustomGender.Female => AppVM.CUE4ParseVM.FemaleIdleAnimations.Random(),
                            _ => null
                        };

                        if (targetMontage is null) break;

                        data.LinkedSequence = await DanceExportData.CreateAnimDataAsync(targetMontage);
                    }

                    /*var masterSkeleton = AppVM.CUE4ParseVM.Provider.LoadObject<USkeleton>("FortniteGame/Content/Characters/Player/Male/Male_Avg_Base/Fortnite_M_Avg_Player_Skeleton");
                    var masterSkeletonExport = ExportHelpers.Skeleton(masterSkeleton);
                    data.Parts.Add(masterSkeletonExport);*/
                    break;
                }
                case EAssetType.Backpack:
                {
                    var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                    ExportHelpers.CharacterParts(parts, data.Parts);
                    break;
                }
                case EAssetType.Pet:
                {
                    var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                    ExportHelpers.CharacterParts(parts, data.Parts);

                    var petData = asset.Get<UObject>("DefaultPet");

                    var petBlueprintClass = petData.Get<UBlueprintGeneratedClass>("PetPrefabClass");
                    var petBlueprintData = await petBlueprintClass.ClassDefaultObject.LoadAsync();

                    var petMeshComponent = petBlueprintData.Get<UObject>("PetMesh");
                    var petMesh = petMeshComponent.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                    petMesh ??= petMeshComponent.GetOrDefault<USkeletalMesh?>("SkinnedAsset");
                    if (petMesh is null) break;

                    var exportPart = ExportHelpers.Mesh<ExportPart>(petMesh);
                    if (exportPart is null) break;
                    exportPart.Part = "PetMesh";

                    var animClass = petMeshComponent.Get<UAnimBlueprintGeneratedClass>("AnimClass");
                    var animClassDefaultObject = await animClass.ClassDefaultObject.LoadAsync();
                    exportPart.ProcessPoses(petMesh, animClassDefaultObject.GetOrDefault<UPoseAsset>("FacePoseAsset"));
                    data.Parts.Add(exportPart);

                    var sequencePlayerNames = animClassDefaultObject.Properties.Where(x => x.Name.Text.Contains("SequencePlayer")).Select(x => x.Name.Text);
                    foreach (var sequencePlayerName in sequencePlayerNames)
                    {
                        var sequencePlayer = animClassDefaultObject.Get<FStructFallback>(sequencePlayerName);
                        var sequence = sequencePlayer.Get<UAnimSequence>("Sequence");
                        if (sequence.Name.Contains("Idle_Lobby", StringComparison.OrdinalIgnoreCase))
                        {
                            data.LinkedSequence = await DanceExportData.CreateAnimDataAsync(sequence, loop: true);
                            break;
                        }
                    }

                    break;
                }
                case EAssetType.Glider:
                {
                    var mesh = asset.Get<USkeletalMesh>("SkeletalMesh");
                    var part = ExportHelpers.Mesh<ExportPart>(mesh);
                    if (part is null) break;
                    var overrides = asset.GetOrDefault("MaterialOverrides", Array.Empty<FStructFallback>());
                    ExportHelpers.OverrideMaterials(overrides, part.OverrideMaterials);
                    data.Parts.Add(part);
                    break;
                }
                case EAssetType.Pickaxe:
                {
                    var weapon = asset.Get<UObject>("WeaponDefinition");
                    ExportHelpers.Weapon(weapon, data.Parts);
                    break;
                }
                case EAssetType.Item:
                {
                    ExportHelpers.Weapon(asset, data.Parts);
                    break;
                }
                case EAssetType.Vehicle:
                {
                    UObject? GetMeshComponent(UBlueprintGeneratedClass? blueprint)
                    {
                        if (blueprint is null) return null;

                        var classDefaultObject = blueprint.ClassDefaultObject.Load();
                        if (classDefaultObject is null) return null;

                        var skeletalMeshComponent = classDefaultObject.GetOrDefault<UObject?>("SkeletalMesh");
                        return skeletalMeshComponent;
                    }

                    var blueprint = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass");

                    var component = GetMeshComponent(blueprint);
                    if (component is null) break;

                    var mesh = component.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                    if (mesh is null)
                    {
                        var superStruct = blueprint.SuperStruct.Load<UBlueprintGeneratedClass>();
                        mesh = GetMeshComponent(superStruct)?.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                    }

                    var part = ExportHelpers.Mesh<ExportMesh>(mesh);
                    if (part is null) break;

                    var overrideMaterials = component.GetOrDefault("OverrideMaterials", Array.Empty<UMaterialInterface?>());
                    for (var i = 0; i < overrideMaterials.Length; i++)
                    {
                        var material = overrideMaterials[i];
                        if (material is null) continue;

                        var exportMaterial = ExportHelpers.CreateExportMaterial(material, i);
                        part.OverrideMaterials.Add(exportMaterial);
                    }

                    data.Parts.Add(part);

                    var exports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(blueprint.GetPathName().SubstringBeforeLast("."));
                    var staticMeshComponents = exports.Where(x => x is UStaticMeshComponent).ToArray();
                    foreach (var staticMeshComponent in staticMeshComponents)
                    {
                        var componentStaticMesh = staticMeshComponent.GetOrDefault<UStaticMesh?>("StaticMesh");
                        if (componentStaticMesh is null) continue;
                        var export = ExportHelpers.Mesh(componentStaticMesh);
                        if (export is null) continue;
                        data.Parts.Add(export);
                    }

                    break;
                }
                case EAssetType.Prop:
                {
                    await data.ProcessLevelSaveRecord(asset);
                    break;
                }
                case EAssetType.Gallery:
                {
                    var recordCollectionLazy = asset.GetOrDefault<FPackageIndex>("PlaysetPropLevelSaveRecordCollection");
                    if (recordCollectionLazy is null || recordCollectionLazy.IsNull || !recordCollectionLazy.TryLoad(out var recordCollection)) break;

                    var props = recordCollection.GetOrDefault<FStructFallback[]>("Items");
                    foreach (var prop in props)
                    {
                        var levelSaveRecord = prop.GetOrDefault<UObject?>("LevelSaveRecord");
                        if (levelSaveRecord is null) continue;

                        var transform = prop.GetOrDefault<FTransform>("Transform");
                        var exportedProps = await data.ProcessLevelSaveRecord(levelSaveRecord);
                        foreach (var exportedProp in exportedProps)
                        {
                            exportedProp.Location += transform.Translation;
                            exportedProp.Rotation += transform.Rotator();
                            exportedProp.Scale *= transform.Scale3D;
                        }
                    }
                    break;
                }
                case EAssetType.Mesh:
                {
                    if (asset is UStaticMesh staticMesh)
                    {
                        ExportHelpers.Mesh(staticMesh, data.Parts);
                    }
                    else if (asset is USkeletalMesh skeletalMesh)
                    {
                        ExportHelpers.Mesh(skeletalMesh, data.Parts);
                    }

                    break;
                }
                case EAssetType.Toy:
                {
                    var blueprint = asset.Get<UBlueprintGeneratedClass>("ToyActorClass");
                    var superStruct = blueprint.SuperStruct.Load<UBlueprintGeneratedClass>();

                    (UStaticMesh?, UMaterialInstanceConstant[]) GetData(UBlueprintGeneratedClass internalBlueprint)
                    {
                        if (internalBlueprint.TryGetValue(out UObject internalComponentHandler, "InheritableComponentHandler"))
                        {
                            var internalRecords = internalComponentHandler.GetOrDefault("Records", Array.Empty<FStructFallback>());
                            foreach (var record in internalRecords)
                            {
                                var template = record.Get<UObject>("ComponentTemplate");
                                if (!template.ExportType.Equals("StaticMeshComponent")) continue;

                                var staticMesh = template.GetOrDefault<UStaticMesh?>("StaticMesh");
                                if (staticMesh is null) continue;
                                var overrideMaterials = template.GetOrDefault("OverrideMaterials", Array.Empty<UMaterialInstanceConstant>());
                                return (staticMesh, overrideMaterials);
                            }
                        }

                        return (null, Array.Empty<UMaterialInstanceConstant>());
                    }

                    var (mesh, materials) = GetData(blueprint);
                    var (superMesh, superMaterials) = GetData(superStruct);
                    if (materials.Length == 0) materials = superMaterials;

                    mesh ??= superMesh;
                    if (mesh is null)
                    {
                        var exports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(blueprint.GetPathName().SubstringBeforeLast("."));
                        var staticMeshComponents = exports.Where(x => x is UStaticMeshComponent).ToArray();
                        foreach (var component in staticMeshComponents)
                        {
                            var componentStaticMesh = component.GetOrDefault<UStaticMesh?>("StaticMesh");
                            if (componentStaticMesh is null) continue;

                            mesh = componentStaticMesh;
                        }
                    }

                    var exportMesh = ExportHelpers.Mesh(mesh);
                    if (exportMesh is null) break;

                    for (var i = 0; i < materials.Length; i++)
                    {
                        var material = materials[i];
                        if (material is null) continue;

                        var exportMaterial = ExportHelpers.CreateExportMaterial(material, i);
                        exportMesh.OverrideMaterials.Add(exportMaterial);
                    }

                    data.Parts.Add(exportMesh);

                    break;
                }
                case EAssetType.Wildlife:
                {
                    ExportHelpers.Mesh(asset as USkeletalMesh, data.Parts);

                    break;
                }
                case EAssetType.Trap:
                {
                    var blueprint = asset.Get<UObject>("BlueprintClass");
                    var exports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(blueprint.GetPathName().SubstringBeforeLast("."));
                    var staticMeshComponents = exports.Where(x => x is UStaticMeshComponent).ToArray();
                    foreach (var component in staticMeshComponents)
                    {
                        var componentStaticMesh = component.GetOrDefault<UStaticMesh?>("StaticMesh");
                        if (componentStaticMesh is null) continue;

                        ExportHelpers.Mesh(componentStaticMesh, data.Parts);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        });
        if (!canContinue) return null;

        await Task.Run(() => data.ProcessStyles(asset, styles));

        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }
}

public static class MeshExportExtensions
{
    public static void ProcessStyles(this MeshExportData data, UObject asset, FStructFallback[] selectedStyles)
    {
        var metaTags = new List<FGameplayTag>();
        var metaTagsToRemove = new List<FGameplayTag>();
        foreach (var style in selectedStyles)
        {
            var tags = style.Get<FStructFallback>("MetaTags");

            var tagsToApply = tags.Get<FGameplayTagContainer>("MetaTagsToApply");
            metaTags.AddRange(tagsToApply.GameplayTags);

            var tagsToRemove = tags.Get<FGameplayTagContainer>("MetaTagsToRemove");
            metaTagsToRemove.AddRange(tagsToRemove.GameplayTags);
        }

        var metaTagContainer = new FGameplayTagContainer(metaTags.Where(tag => !metaTagsToRemove.Contains(tag)).ToArray());

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
                    if (metaTagContainer.MatchesQuery(metaTagQuery))
                    {
                        ExportStyleData(option, data);
                    }
                }
            }
        }

        foreach (var style in selectedStyles)
        {
            ExportStyleData(style, data);
        }
    }

    private static void ExportStyleData(FStructFallback style, MeshExportData data)
    {
        ExportHelpers.CharacterParts(style.GetOrDefault("VariantParts", Array.Empty<UObject>()), data.StyleParts);
        ExportHelpers.OverrideMaterials(style.GetOrDefault("VariantMaterials", Array.Empty<FStructFallback>()), data.StyleMaterials);
        ExportHelpers.OverrideMeshes(style.GetOrDefault("VariantMeshes", Array.Empty<FStructFallback>()), data.StyleMeshes);
        ExportHelpers.OverrideParameters(style.GetOrDefault("VariantMaterialParams", Array.Empty<FStructFallback>()), data.StyleMaterialParams);
    }

    public static async Task<List<ExportMesh>> ProcessLevelSaveRecord(this MeshExportData data, UObject levelSaveRecord)
    {
        var exports = new List<ExportMesh>();
        
        var actorSaveRecord = levelSaveRecord.Get<ULevelSaveRecord>("ActorSaveRecord");
        var templateRecords = new List<FActorTemplateRecord?>();
        foreach (var tag in actorSaveRecord.Get<UScriptMap>("TemplateRecords").Properties)
        {
            var propValue = tag.Value?.GetValue(typeof(FActorTemplateRecord));
            templateRecords.Add(propValue as FActorTemplateRecord);
        }

        foreach (var templateRecord in templateRecords)
        {
            if (templateRecord is null) continue;
            var actor = templateRecord.ActorClass.Load<UBlueprintGeneratedClass>();
            var classDefaultObject = await actor.ClassDefaultObject.LoadAsync();

            string? targetMaterialPath = null;
            if (classDefaultObject.TryGetValue(out UStaticMesh staticMesh, "StaticMesh"))
            {
                var export = ExportHelpers.Mesh(staticMesh)!;
                exports.Add(export);

                targetMaterialPath = export.Materials.FirstOrDefault()?.MaterialPath;
            }
            else
            {
                var objects = AppVM.CUE4ParseVM.Provider.LoadAllObjects(actor.GetPathName().SubstringBeforeLast("."));
                var staticMeshComponents = objects.Where(x => x is UStaticMeshComponent).ToArray();
                if (!staticMeshComponents.Any()) continue;
                
                foreach (var component in staticMeshComponents)
                {
                    var componentStaticMesh = component.GetOrDefault<UStaticMesh?>("StaticMesh");
                    if (componentStaticMesh is null) continue;
                    var export = ExportHelpers.Mesh(componentStaticMesh)!;
                    exports.Add(export);

                    targetMaterialPath = export.Materials.FirstOrDefault()?.MaterialPath;
                }
            }

            // EXTRA MESHES
            if (classDefaultObject.TryGetValue(out UStaticMesh doorMesh, "DoorMesh"))
            {
                var export = ExportHelpers.Mesh(doorMesh)!;
                var doorOffset = classDefaultObject.GetOrDefault("DoorOffset", FVector.ZeroVector);
                export.Location = doorOffset;
                exports.Add(export);

                if (classDefaultObject.GetOrDefault<bool>("bDoubleDoor"))
                {
                    var doubleDoorExport = ExportHelpers.Mesh(doorMesh)!;
                    doubleDoorExport.Location = doorOffset;
                    doubleDoorExport.Location.X = -doubleDoorExport.Location.X;
                    doubleDoorExport.Scale.X = -1;
                    exports.Add(doubleDoorExport);
                }
            }

            var actorData = templateRecord.ReadActorData(actorSaveRecord.Owner, actorSaveRecord.SaveVersion);
            if (!actorData.TryGetAllValues(out string[] textureDataPaths, "TextureData"))
            {
                var referenceTable = templateRecord.ActorDataReferenceTable;
                textureDataPaths = referenceTable.Select(x => x.AssetPathName.Text).ToArray();
            }

            for (var idx = 0; idx < textureDataPaths.Length; idx++)
            {
                var textureDataPath = textureDataPaths[idx];
                if (string.IsNullOrEmpty(textureDataPath) || textureDataPath.Equals("None", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(targetMaterialPath)) continue;
                
                var textureData = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync<UBuildingTextureData>(textureDataPath);
                data.StyleMaterialParams.Add(textureData.ToExportMaterialParams(idx, targetMaterialPath));
            }
        }
        
        data.Parts.AddRange(exports);
        return exports;
    }
}