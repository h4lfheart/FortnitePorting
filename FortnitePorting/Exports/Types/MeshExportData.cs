using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.AppUtils;

namespace FortnitePorting.Exports.Types;

public class MeshExportData : ExportDataBase
{
    public List<ExportPart> Parts = new();
    public List<ExportPart> StyleParts = new();
    public List<ExportMaterial> StyleMaterials = new();
    public List<ExportMeshOverride> StyleMeshes = new();

    public static async Task<MeshExportData?> Create(UObject asset, EAssetType assetType, FStructFallback[] styles)
    {
        var data = new MeshExportData();
        data.Name = asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        data.Type = assetType.ToString();
        var canContinue = await Task.Run(() =>
        {
            switch (assetType)
            {
                case EAssetType.Outfit:
                {
                    var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                    ExportHelpers.CharacterParts(parts, data.Parts);
                    break;
                }
                case EAssetType.Backpack:
                {
                    var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                    ExportHelpers.CharacterParts(parts, data.Parts);
                    break;
                }
                case EAssetType.Glider:
                {
                    var mesh = asset.Get<USkeletalMesh>("SkeletalMesh");
                    var addedIndex = ExportHelpers.Mesh(mesh, data.Parts);
                    var part = data.Parts[addedIndex];
                    var overrides = asset.GetOrDefault("MaterialOverrides", Array.Empty<FStructFallback>());
                    ExportHelpers.OverrideMaterials(overrides, part.OverrideMaterials);
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
                case EAssetType.Prop:
                {
                    var actorSaveRecord = asset.Get<ULevelSaveRecord>("ActorSaveRecord");
                    FActorTemplateRecord? templateRecord = null;
                    foreach (var tag in actorSaveRecord.Get<UScriptMap>("TemplateRecords").Properties)
                    {
                        var propValue = tag.Value?.GetValue(typeof(FActorTemplateRecord));
                        templateRecord = propValue as FActorTemplateRecord;
                    }
                    
                    var actor = templateRecord?.ActorClass.Load<UBlueprintGeneratedClass>();
                    var actorComponents = actor?.ClassDefaultObject.Load();
                    var staticMesh = actorComponents?.GetOrDefault<UStaticMesh?>("StaticMesh");
                    if (staticMesh is null)
                    {
                        AppLog.Error($"StaticMesh for prop {data.Name} could not be found");
                        return false;
                    }
                    ExportHelpers.Mesh(staticMesh, data.Parts);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        });
        if (!canContinue) return null;
        
        data.ProcessStyles(asset, styles);

        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }
    
}

public static class MeshExportExtensions
{
    public static void ProcessStyles(this MeshExportData data, UObject asset, FStructFallback[] selectedStyles)
    {
        var totalMetaTags = new List<string>();
        var metaTagsToApply = new List<string>();
        var metaTagsToRemove= new List<string>();
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
        // TODO VARIANT PARAMS
    }
}