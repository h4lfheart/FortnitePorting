using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
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
        data.Name = assetType == EAssetType.Mesh ? asset.Name : asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        data.Type = assetType.ToString();
        var canContinue = await Task.Run(async () =>
        {
            switch (assetType)
            {
                case EAssetType.Outfit:
                {
                    var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                    var exportedParts = ExportHelpers.CharacterParts(parts, data.Parts);

                    if (asset.TryGetValue(out UObject heroDefinition, "HeroDefinition"))
                    {
                        var frontendAnimMontage = heroDefinition.GetOrDefault<UAnimMontage?>("FrontendAnimMontageIdleOverride");
                        if (frontendAnimMontage is not null)
                        {
                            data.LinkedSequence = await DanceExportData.CreateAnimDataAsync(frontendAnimMontage);
                        }
                        else
                        {
                            var bodyPart = exportedParts.First(x => x.Part.Equals("Body"));
                            var targetMontage = bodyPart.GenderPermitted switch
                            {
                                EFortCustomGender.Male => AppVM.CUE4ParseVM.MaleIdleAnimations.Random(),
                                EFortCustomGender.Female => AppVM.CUE4ParseVM.FemaleIdleAnimations.Random(),
                                _ => null
                            };

                            if (targetMontage is null) break;

                            data.LinkedSequence = await DanceExportData.CreateAnimDataAsync(targetMontage);
                        }
                    }

                    var masterSkeleton = AppVM.CUE4ParseVM.Provider.LoadObject<USkeleton>("FortniteGame/Content/Characters/Player/Male/Male_Avg_Base/Fortnite_M_Avg_Player_Skeleton");
                    var masterSkeletonExport = ExportHelpers.Skeleton(masterSkeleton);
                    data.Parts.Add(masterSkeletonExport);
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
                case EAssetType.Weapon:
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

                    var exports = AppVM.CUE4ParseVM.Provider.LoadObjectExports(blueprint.GetPathName().SubstringBeforeLast("."));
                    var staticMeshComponents = exports.Where(x => x.ExportType == "StaticMeshComponent").ToArray();
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
                    var actorSaveRecord = asset.Get<ULevelSaveRecord>("ActorSaveRecord");
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

                        if (classDefaultObject.TryGetValue(out UStaticMesh staticMesh, "StaticMesh"))
                        {
                            var export = ExportHelpers.Mesh(staticMesh)!;
                            data.Parts.Add(export);
                        }
                        else
                        {
                            var exports = AppVM.CUE4ParseVM.Provider.LoadObjectExports(actor.GetPathName().SubstringBeforeLast("."));
                            var staticMeshComponents = exports.Where(x => x.ExportType == "StaticMeshComponent").ToArray();
                            if (!staticMeshComponents.Any())
                                AppLog.Error($"StaticMesh could not be found in actor {actor.Name} for prop {data.Name}");
                            foreach (var component in staticMeshComponents)
                            {
                                var componentStaticMesh = component.GetOrDefault<UStaticMesh?>("StaticMesh");
                                if (componentStaticMesh is null) continue;
                                var export = ExportHelpers.Mesh(componentStaticMesh)!;
                                data.Parts.Add(export);
                            }
                        }

                        // EXTRA MESHES
                        if (classDefaultObject.TryGetValue(out UStaticMesh doorMesh, "DoorMesh"))
                        {
                            var export = ExportHelpers.Mesh(doorMesh)!;
                            var doorOffset = classDefaultObject.GetOrDefault("DoorOffset", FVector.ZeroVector);
                            export.Offset = doorOffset;
                            data.Parts.Add(export);

                            if (classDefaultObject.GetOrDefault<bool>("bDoubleDoor"))
                            {
                                var doubleDoorExport = ExportHelpers.Mesh(doorMesh)!;
                                doubleDoorExport.Offset = doorOffset;
                                doubleDoorExport.Offset.X = -doubleDoorExport.Offset.X;
                                doubleDoorExport.Scale.X = -1;
                                data.Parts.Add(doubleDoorExport);
                            }
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
        // apply gameplay tags for selected styles
        var totalMetaTags = new List<string>();
        var metaTagsToApply = new List<string>();
        var metaTagsToRemove = new List<string>();
        foreach (var style in selectedStyles)
        {
            var tags = style.Get<FStructFallback>("MetaTags");

            var tagstoApply = tags.Get<FGameplayTagContainer>("MetaTagsToApply");
            metaTagsToApply.AddRange(tagstoApply.GameplayTags.Select(x => x.Text));

            var tagsToRemove = tags.Get<FGameplayTagContainer>("MetaTagsToRemove");
            metaTagsToRemove.AddRange(tagsToRemove.GameplayTags.Select(x => x.Text));
        }

        totalMetaTags.AddRange(metaTagsToApply);
        metaTagsToRemove.ForEach(tag => totalMetaTags.RemoveAll(x => x.Equals(tag, StringComparison.OrdinalIgnoreCase)));

        // figure out if the selected gameplay tags above match any of the tag driven styles
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
                    var metaTagQuery = condition.Get<FStructFallback>("MetaTagQuery");
                    var tagDictionary = metaTagQuery.Get<FStructFallback[]>("TagDictionary");
                    var requiredTags = tagDictionary.Select(x => x.Get<FName>("TagName").Text).ToList();
                    if (requiredTags.All(x => totalMetaTags.Contains(x)))
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
}