using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using Serilog;

namespace FortnitePorting.Models.Assets.Asset;

public partial class AssetInfo : Base.BaseAssetInfo
{
    public new AssetItem Asset
    {
        get => (AssetItem) base.Asset;
        private init => base.Asset = value;
    }
    
    public AssetInfo(AssetItem asset)
    {
        Asset = asset;
        if (Asset.CreationData.Object is null) return;
        
        var styles = Asset.CreationData.Object.GetOrDefault("ItemVariants", Array.Empty<UObject>());
        foreach (var style in styles)
        {
            var channel = style.GetOrDefault("VariantChannelName", new FText("Style")).Text.ToLower().TitleCase();
            var optionsName = style.ExportType switch
            {
                "FortCosmeticCharacterPartVariant" => "PartOptions",
                "FortCosmeticMaterialVariant" => "MaterialOptions",
                "FortCosmeticParticleVariant" => "ParticleOptions",
                "FortCosmeticMeshVariant" => "MeshOptions",
                "FortCosmeticGameplayTagVariant" => "GenericTagOptions",
                _ => null
            };

            if (optionsName is null) continue;

            var options = style.Get<FStructFallback[]>(optionsName);
            if (options.Length == 0) continue;

            var styleInfo = new AssetStyleInfo(channel, options, Asset.IconDisplayImage);
            if (styleInfo.StyleDatas.Count > 0) StyleInfos.Add(styleInfo);
        }

        if (Asset.CreationData.ExportType is EExportType.Emote)
        {
            var maleBaseAnimation = Asset.CreationData.Object.GetOrDefault<UAnimMontage?>("Animation");
            maleBaseAnimation ??= Asset.CreationData.Object.GetOrDefault<UAnimMontage?>("FrontEndAnimation");
            
            var femaleBaseAnimation = Asset.CreationData.Object.GetOrDefault<UAnimMontage?>("AnimationFemaleOverride");
            var animationOverrides = Asset.CreationData.Object.GetOrDefault<FStructFallback[]>("AnimationOverrides", []);

            var sizedAnimations = new Dictionary<string, UAnimMontage>();

            if (maleBaseAnimation is not null)
                sizedAnimations.Add($"Male Medium ({maleBaseAnimation.Name})", maleBaseAnimation);
            
            if (femaleBaseAnimation is not null)
                sizedAnimations.Add($"Female Medium ({femaleBaseAnimation.Name})", femaleBaseAnimation);

            foreach (var animationOverride in animationOverrides)
            {
                var gender = animationOverride.Get<FName?>("Gender")?.Text.SubstringAfter("::");
                var bodyType = animationOverride.Get<FName?>("BodyType")?.Text.SubstringAfter("::");
                var montage = animationOverride.GetOrDefault<UAnimMontage?>("EmoteMontage");
                if (gender is null || bodyType is null || montage is null) continue;
                
                sizedAnimations.Add($"{gender} {bodyType} ({montage.Name})", montage);
            }

            if (sizedAnimations.Count > 0)
            {
                var sizedStyleDatas = sizedAnimations
                    .Select(kvp => new AnimStyleData(kvp.Key, kvp.Value))
                    .ToArray();
            
                StyleInfos.Add(new AssetStyleInfo("Animation Type", sizedStyleDatas));
            }
        }
    }
    
    public AssetInfo(AssetItem asset, IEnumerable<string> stylePaths)
    {
        Asset = asset;
        if (Asset.CreationData.Object is null) return;

        var styleObjects = new List<UObject>();
        foreach (var stylePath in stylePaths)
        {
            if (UEParse.Provider.TryLoadPackageObject(stylePath, out var styleObject))
            {
                styleObjects.Add(styleObject);
            }
        }
        
        var styleInfo = new AssetStyleInfo("Styles", styleObjects, Asset.IconDisplayImage);
        if (styleInfo.StyleDatas.Count > 0) StyleInfos.Add(styleInfo);
    }
    
    public BaseStyleData[] GetSelectedStyles()
    {
        return StyleInfos
            .SelectMany<AssetStyleInfo, BaseStyleData>(info => info.MultiSelect ? info.SelectedItems : [info.SelectedStyle])
            .ToArray();
    }
    
    public BaseStyleData[] GetAllStyles()
    {
        return StyleInfos
            .SelectMany<AssetStyleInfo, BaseStyleData>(info => info.StyleDatas)
            .ToArray();
    }
}