using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Views.Controls;

public partial class PropExpander
{
    public ObservableCollection<AssetSelectorItem> Props { get; set; } = new();

    public PropExpander(string name, UTexture2D previewTexture)
    {
        InitializeComponent();

        GalleryName.Text = name;
        Icon.Source = previewTexture.Decode()?.ToBitmapImage();
    }

    private void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;

        var selected = (AssetSelectorItem) listBox.SelectedItem;
        if (selected.IsRandom)
        {
            listBox.SelectedIndex = App.RandomGenerator.Next(0, listBox.Items.Count);
            return;
        }

        AppVM.NewMainVM.Styles.Clear();
        AppVM.NewMainVM.CurrentAsset = selected;

        var styles = selected.Asset.GetOrDefault("ItemVariants", Array.Empty<UObject>());
        foreach (var style in styles)
        {
            var channel = style.GetOrDefault("VariantChannelName", new FText("Unknown")).Text.ToLower().TitleCase();
            var optionsName = style.ExportType switch
            {
                "FortCosmeticCharacterPartVariant" => "PartOptions",
                "FortCosmeticMaterialVariant" => "MaterialOptions",
                "FortCosmeticParticleVariant" => "ParticleOptions",
                "FortCosmeticMeshVariant" => "MeshOptions",
                _ => null
            };

            if (optionsName is null) continue;

            var options = style.Get<FStructFallback[]>(optionsName);
            if (options.Length == 0) continue;

            var styleSelector = new StyleSelector(channel, options, selected.IconBitmap);
            if (styleSelector.Options.Items.Count == 0) continue;
            AppVM.NewMainVM.Styles.Add(styleSelector);
        }
    }
}