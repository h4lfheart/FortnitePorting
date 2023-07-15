using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using StyleSelector = FortnitePorting.Views.Controls.StyleSelector;

namespace FortnitePorting.Views;

public partial class MainView
{
    public MainView()
    {
        InitializeComponent();
        AppVM.MainVM = new MainViewModel();
        DataContext = AppVM.MainVM;

        Title = $"Fortnite Porting - v{Globals.VERSION}";
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        AppVM.AssetHandlerVM = new AssetHandlerViewModel();
        await AppVM.AssetHandlerVM.Initialize();
    }

    private async void OnAssetTypeClick(object sender, RoutedEventArgs e)
    {
        if (AppVM.AssetHandlerVM is null) return;

        var clickedButton = (ToggleButton) sender;
        var assetType = (EAssetType) clickedButton.Tag;
        if (AppVM.MainVM.CurrentAssetType == assetType) return;

        AssetDisplayGrid.SelectionMode = assetType is (EAssetType.Prop or EAssetType.Gallery) ? SelectionMode.Extended : SelectionMode.Single;
        AppVM.MainVM.CurrentAsset = null;
        AppVM.MainVM.CurrentAssetType = assetType;
        DiscordService.Update(assetType);

        // uncheck other buttons
        var buttons = AssetTypePanel.Children.OfType<ToggleButton>();
        foreach (var button in buttons)
        {
            if (button == clickedButton) continue;
            button.IsChecked = false;
        }

        var handlers = AppVM.AssetHandlerVM.Handlers;
        foreach (var (handlerType, handlerData) in handlers)
        {
            if (handlerType == assetType && !AppVM.MainVM.IsPaused)
            {
                handlerData.PauseState.Unpause();
            }
            else
            {
                handlerData.PauseState.Pause();
            }
        }

        if (assetType is EAssetType.Mesh)
        {
            if (AppVM.MeshVM is not null && AppVM.MeshVM.HasStarted) return;
            AppVM.MeshVM = new MeshAssetViewModel();
            await AppVM.MeshVM.Initialize();
        }
        else
        {
            if (assetType is not EAssetType.Gallery) AssetDisplayGrid.ItemsSource = handlers[assetType].TargetCollection;
            if (!handlers[assetType].HasStarted)
            {
                await handlers[assetType].Execute();
            }
        }
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

        AppVM.MainVM.CurrentAsset = selected;
        AppVM.MainVM.ExtendedAssets.Clear();
        AppVM.MainVM.Styles.Clear();

        if (selected.Type is EAssetType.Prop)
        {
            AppVM.MainVM.OptionTabText = "SELECTED PROPS";
            AppVM.MainVM.ExtendedAssets = listBox.SelectedItems.OfType<IExportableAsset>().ToList();
            AppVM.MainVM.Styles.Add(new StyleSelector(AppVM.MainVM.ExtendedAssets));
            return;
        }

        AppVM.MainVM.OptionTabText = "OPTIONS";
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
            AppVM.MainVM.Styles.Add(styleSelector);
        }
    }

    private void OnToggleConsoleChecked(object sender, RoutedEventArgs e)
    {
        var menuItem = (MenuItem) sender;
        var show = menuItem.IsChecked;
        AppSettings.Current.ShowConsole = show;
        App.ToggleConsole(show);
    }

    private void FunkyScrollFix(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;
        switch (e.Delta)
        {
            case < 0:
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 88);
                break;
            case > 0:
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 88);
                break;
        }
    }

    private void RefreshFilters()
    {
        AssetDisplayGrid.Items.Filter = o =>
        {
            var asset = (AssetSelectorItem) o;
            return asset.Match(AppVM.MainVM.SearchFilter) && AppVM.MainVM.Filters.All(x => x.Invoke(asset));
        };
        AssetDisplayGrid.Items.Refresh();

        if (AppVM.MainVM.CurrentAssetType is EAssetType.Gallery)
        {
            GalleryItemsControl.Items.Filter = o =>
            {
                var asset = (PropExpander) o;
                return AppHelper.Filter(asset.GalleryName.Text, AppVM.MainVM.SearchFilter);
            };
            GalleryItemsControl.Items.Refresh();
        }

        if (AppVM.MainVM.CurrentAssetType is EAssetType.Mesh)
        {
            AssetFlatView.Items.Filter = o =>
            {
                var asset = (AssetItem) o;
                return AppHelper.Filter(asset.Path, AppVM.MainVM.SearchFilter);
            };
            AssetFlatView.Items.Refresh();
        }
    }

    private void OnFilterItemChecked(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox) sender;
        if (checkBox.Tag is null) return;
        if (!checkBox.IsChecked.HasValue) return;

        AppVM.MainVM.ModifyFilters(checkBox.Tag.ToString()!, checkBox.IsChecked.Value);
        RefreshFilters();
    }

    private void OnClearFiltersClicked(object sender, RoutedEventArgs e)
    {
        AppVM.MainVM.Filters.Clear();
        foreach (var child in FilterPanel.Children)
        {
            if (child is not CheckBox checkBox) continue;
            checkBox.IsChecked = false;
        }

        RefreshFilters();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshFilters();
    }

    private void RefreshSorting()
    {
        AssetDisplayGrid.Items.SortDescriptions.Clear();
        AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("IsRandom", ListSortDirection.Descending));

        switch (AppVM.MainVM.SortType)
        {
            case ESortType.Default:
                if (AppVM.MainVM.CurrentAssetType is EAssetType.Gallery) break;
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("ID", GetProperSort(ListSortDirection.Ascending)));
                break;
            case ESortType.AZ:
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("DisplayName", GetProperSort(ListSortDirection.Ascending)));
                break;
            case ESortType.Season:
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("SeasonNumber", GetProperSort(ListSortDirection.Ascending)));
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("Rarity", GetProperSort(ListSortDirection.Ascending)));
                break;
            case ESortType.Rarity:
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("Rarity", GetProperSort(ListSortDirection.Ascending)));
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("ID", GetProperSort(ListSortDirection.Ascending)));
                break;
            case ESortType.Series:
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("Series", GetProperSort(ListSortDirection.Descending)));
                AssetDisplayGrid.Items.SortDescriptions.Add(new SortDescription("Rarity", GetProperSort(ListSortDirection.Descending)));
                break;
        }
    }

    private ListSortDirection GetProperSort(ListSortDirection direction)
    {
        return direction switch
        {
            ListSortDirection.Ascending => AppVM.MainVM.Ascending ? ListSortDirection.Descending : direction,
            ListSortDirection.Descending => AppVM.MainVM.Ascending ? ListSortDirection.Ascending : direction
        };
    }

    private void OnSortSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshSorting();
    }

    private void OnAscendingDescendingClicked(object sender, RoutedEventArgs e)
    {
        var newValue = !AppVM.MainVM.Ascending;
        AppVM.MainVM.Ascending = newValue;

        var button = (Button) sender;
        var image = (IconBecauseImLazy) button.Content;
        image.RenderTransform = new RotateTransform(newValue ? 180 : 0);

        RefreshSorting();
    }

    private void OnPauseLoadingSwitched(object sender, RoutedEventArgs e)
    {
        if (AppVM.AssetHandlerVM is null) return;

        var toggleSwitch = (ToggleButton) sender;
        var pauseValue = toggleSwitch.IsChecked.HasValue && toggleSwitch.IsChecked.Value;
        foreach (var (_, handler) in AppVM.AssetHandlerVM.Handlers)
        {
            handler.PauseState.IsPaused = pauseValue;
        }

        AppVM.MainVM.IsPaused = pauseValue;
    }

    private async void AssetFolderTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var treeView = (TreeView) sender;
        var treeItem = (TreeItem) treeView.SelectedItem;
        if (treeItem is null) return;
        if (treeItem.AssetType == ETreeItemType.Folder) return;

        await AppVM.MainVM.SetupMeshSelection(treeItem.FullPath!);
    }

    private async void AssetFlatView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listBox = (ListBox) sender;
        var selectedItem = (AssetItem) listBox.SelectedItem;
        if (selectedItem is null) return;

        await AppVM.MainVM.SetupMeshSelection(listBox.SelectedItems.OfType<AssetItem>().ToArray());
    }

    private void AssetFlatView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var listBox = (ListBox) sender;
        var selectedItem = (AssetItem) listBox.SelectedItem;
        if (selectedItem is null) return;

        JumpToAsset(selectedItem.PathWithoutExtension);
    }

    private void JumpToAsset(string directory)
    {
        var children = AppVM.MainVM.Meshes;

        var i = 0;
        var folders = directory.Split('/');
        while (true)
        {
            foreach (var folder in children)
            {
                if (!folder.Header.Equals(folders[i], StringComparison.OrdinalIgnoreCase))
                    continue;

                if (folder.AssetType == ETreeItemType.Asset)
                {
                    folder.IsSelected = true;
                    return;
                }

                folder.IsExpanded = true;
                children = folder.Children;
                break;
            }

            i++;
            if (children.Count == 0) break;
        }
    }
}