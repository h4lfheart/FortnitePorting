using System;
using System.Linq;
using Avalonia.Controls;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Extensions;

namespace FortnitePorting.Controls.Assets;

public partial class AssetOptions : UserControl
{
    public AssetItem AssetItem { get; set; }

    public AssetOptions(AssetItem assetItem)
    {
        InitializeComponent();

        AssetItem = assetItem;

        Styles.Items.Clear();
        var styles = AssetItem.Asset.GetOrDefault("ItemVariants", Array.Empty<UObject>());
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

            var styleSelector = new StyleItem(channel, options, AssetItem.IconBitmap);
            if (styleSelector.Styles.Count == 0) continue;
            Styles.Items.Add(styleSelector);
        }
    }

    public FStructFallback[] GetSelectedStyles()
    {
        return Styles.Items
            .Cast<StyleItem>()
            .Select(x => (StyleEntry) (x.StylesListBox.SelectedItem ?? x.Styles.FirstOrDefault())!)
            .RemoveNull()
            .Select(x => x.StyleInfo)
            .ToArray();
    }
}