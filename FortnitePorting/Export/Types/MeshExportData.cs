using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.GameplayTags;
using FortnitePorting.Extensions;

namespace FortnitePorting.Export.Types;

public class MeshExportData : ExportDataBase
{
    public readonly List<ExportMesh> Meshes = new();
    public readonly List<ExportMesh> OverrideMeshes = new();
    public MeshExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportType exportType) : base(name, asset, styles, type, exportType)
    {
        switch (type)
        {
            case EAssetType.Outfit:
            {
                var characterParts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                foreach (var part in characterParts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                }
                break;
            }
            case EAssetType.Backpack:
                break;
            case EAssetType.Pickaxe:
                break;
            case EAssetType.Glider:
                break;
            case EAssetType.Pet:
                break;
            case EAssetType.Toy:
                break;
            case EAssetType.Spray:
                break;
            case EAssetType.Banner:
                break;
            case EAssetType.LoadingScreen:
                break;
            case EAssetType.Emote:
                break;
            case EAssetType.Prop:
                break;
            case EAssetType.Gallery:
                break;
            case EAssetType.Item:
                break;
            case EAssetType.Trap:
                break;
            case EAssetType.Vehicle:
                break;
            case EAssetType.Wildlife:
                break;
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
                    if (metaTags.MatchesQuery(metaTagQuery))
                    {
                        ExportStyleData(option);
                    }
                }
            }
        }
        
        foreach (var style in styles)
        {
            ExportStyleData(style);
        }
    }

    private void ExportStyleData(FStructFallback style)
    {
        var variantParts = style.GetOrDefault("VariantParts", Array.Empty<UObject>());
        foreach (var part in variantParts)
        {
            Meshes.AddIfNotNull(Exporter.CharacterPart(part));
        }
    }
}