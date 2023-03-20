using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Views.Extensions;
using SharpGLTF.Schema2;

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

        AppVM.MainVM.Styles.Clear();
        AppVM.MainVM.TabModeText = "SELECTED PROPS";
        if (listBox.SelectedItems.Count == 0) return;
        
        AppVM.MainVM.CurrentAsset = selected;
        AppVM.MainVM.ExtendedAssets.Clear();
        AppVM.MainVM.ExtendedAssets = listBox.SelectedItems.OfType<IExportableAsset>().ToList();
        AppVM.MainVM.Styles.Add(new StyleSelector(AppVM.MainVM.ExtendedAssets));
    }
}