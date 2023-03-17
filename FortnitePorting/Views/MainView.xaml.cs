using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
    public static MainView YesWeDogs;

    public MainView()
    {
        InitializeComponent();
        AppVM.MainVM = new MainViewModel();
        DataContext = AppVM.MainVM;

        AppLog.Logger = LoggerBox;
        Title = $"Fortnite Porting - v{Globals.VERSION}";
        Icon = new BitmapImage(new Uri(AppSettings.Current.LightMode ? "pack://application:,,,/FortnitePorting-Dark.ico" : "pack://application:,,,/FortnitePorting.ico", UriKind.RelativeOrAbsolute));
        YesWeDogs = this;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AppSettings.Current.ArchivePath) && AppSettings.Current.InstallType == EInstallType.Local)
        {
            AppHelper.OpenWindow<StartupView>();
            return;
        }

        var (updateAvailable, updateVersion) = UpdateService.GetStats();
        if (DateTime.Now >= AppSettings.Current.LastUpdateAskTime.AddDays(1) || updateVersion > AppSettings.Current.LastKnownUpdateVersion)
        {
            AppSettings.Current.LastKnownUpdateVersion = updateVersion;
            UpdateService.Start(automaticCheck: true);
            AppSettings.Current.LastUpdateAskTime = DateTime.Now;
        }

        if (AppSettings.Current.JustUpdated && !updateAvailable)
        {
            AppHelper.OpenWindow<PluginUpdateView>();
            AppSettings.Current.JustUpdated = false;
        }

        await AppVM.MainVM.Initialize();
    }

    private async void OnAssetTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppVM.AssetHandlerVM is null) return;
        if (sender is not TabControl tabControl) return;

        var assetType = (EAssetType) tabControl.SelectedIndex;
        if (AppVM.MainVM.CurrentAssetType == assetType) return;

        AppVM.MainVM.ExtendedAssets.Clear();
        AppVM.MainVM.CurrentAssetType = assetType;
        DiscordService.Update(assetType);
        
        var handlers = AppVM.AssetHandlerVM.Handlers;
        foreach (var (handlerType, handlerData) in handlers)
        {
            if (handlerType == assetType && !PauseButton.IsChecked.Value)
            {
                handlerData.PauseState.Unpause();
            }
            else
            {
                handlerData.PauseState.Pause();
            }
        }

        if (assetType == EAssetType.Mesh)
        {
            if (AppVM.MeshVM is not null && AppVM.MeshVM.HasStarted) return;
            AppVM.MeshVM = new MeshAssetViewModel();
            await AppVM.MeshVM.Initialize();
            return;
        }

        if (!handlers[assetType].HasStarted)
        {
            await handlers[assetType].Execute();
        }
    }

    private void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            if (listBox.SelectedItem is null) return;
            var selected = (AssetSelectorItem)listBox.SelectedItem;

            AppVM.MainVM.Styles.Clear();
            if (selected.Type == EAssetType.Prop)
            {
                AppVM.MainVM.TabModeText = "SELECTED PROPS";
                if (listBox.SelectedItems.Count == 0) return;
                AppVM.MainVM.CurrentAsset = selected;
                AppVM.MainVM.ExtendedAssets.Clear();
                AppVM.MainVM.ExtendedAssets = listBox.SelectedItems.OfType<IExportableAsset>().ToList();
                AppVM.MainVM.Styles.Add(new StyleSelector(AppVM.MainVM.ExtendedAssets));
                return;
            }

            if (selected.IsRandom)
            {
                listBox.SelectedIndex = App.RandomGenerator.Next(0, listBox.Items.Count);
                return;
            }

            AppVM.MainVM.ExtendedAssets.Clear();
            AppVM.MainVM.CurrentAsset = selected;
            AppVM.MainVM.TabModeText = "STYLES";

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
    }

    private void StupidIdiotBadScroll(object sender, MouseWheelEventArgs e)
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

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchBox = (TextBox)sender;
        AppVM.MainVM.SearchFilter = searchBox.Text;
        RefreshFilters();
    }

    public void RefreshFilters()
    {
        AssetFlatView.Items.Filter = o =>
        {
            var asset = (AssetItem)o;
            return AppHelper.Filter(asset.Path, AppVM.MainVM.SearchFilter);
        };
        AssetFlatView.Items.Refresh();

        foreach (var tab in AssetControls.Items.OfType<TabItem>())
        {
            if (tab.Content is not ListBox listBox) continue;

            listBox.Items.Filter = o =>
            {
                var asset = (AssetSelectorItem)o;
                return asset.Match(AppVM.MainVM.SearchFilter) && AppVM.MainVM.Filters.All(x => x.Invoke(asset));
            };
            listBox.Items.Refresh();
        }
    }

    private void OnSortSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshSorting();
    }

    public void RefreshSorting()
    {
        foreach (var tab in AssetControls.Items.OfType<TabItem>())
        {
            if (tab.Content is not ListBox listBox) continue;
            listBox.Items.SortDescriptions.Clear();
            listBox.Items.SortDescriptions.Add(new SortDescription("IsRandom", ListSortDirection.Descending));
            switch (AppVM.MainVM.SortType)
            {
                case ESortType.Default:
                    listBox.Items.SortDescriptions.Add(new SortDescription("ID", GetProperSort(ListSortDirection.Ascending)));
                    break;
                case ESortType.AZ:
                    listBox.Items.SortDescriptions.Add(new SortDescription("DisplayName", GetProperSort(ListSortDirection.Ascending)));
                    break;
                case ESortType.Season:
                    listBox.Items.SortDescriptions.Add(new SortDescription("SeasonNumber", GetProperSort(ListSortDirection.Ascending)));
                    listBox.Items.SortDescriptions.Add(new SortDescription("Rarity", GetProperSort(ListSortDirection.Ascending)));
                    break;
                case ESortType.Rarity:
                    listBox.Items.SortDescriptions.Add(new SortDescription("Rarity", GetProperSort(ListSortDirection.Ascending)));
                    listBox.Items.SortDescriptions.Add(new SortDescription("ID", GetProperSort(ListSortDirection.Ascending)));
                    break;
                case ESortType.Series:
                    listBox.Items.SortDescriptions.Add(new SortDescription("Series", GetProperSort(ListSortDirection.Descending)));
                    listBox.Items.SortDescriptions.Add(new SortDescription("Rarity", GetProperSort(ListSortDirection.Descending)));
                    break;
            }
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

    private void OnShowConsoleChecked(object sender, RoutedEventArgs e)
    {
        var menuItem = (MenuItem)sender;
        var show = menuItem.IsChecked;
        AppSettings.Current.ShowConsole = show;
        App.ToggleConsole(show);
    }

    private void ToggleButton_OnChanged(object sender, RoutedEventArgs e)
    {
        RefreshSorting();
    }

    private void OnFilterItemChecked(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        if (checkBox.Tag is null) return;
        if (!checkBox.IsChecked.HasValue) return;

        AppVM.MainVM.ModifyFilters(checkBox.Tag.ToString()!, checkBox.IsChecked.Value);
        RefreshFilters();
    }

    private void ClearFilters_OnClicked(object sender, RoutedEventArgs e)
    {
        foreach (var child in FilterPanel.Children)
        {
            if (child is not CheckBox checkBox) continue;
            checkBox.IsChecked = false;
        }

        RefreshFilters();
    }

    public void JumpToAsset(string directory)
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

    private async void AssetFolderTree_OnSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> routedPropertyChangedEventArgs)
    {
        var treeView = (TreeView)sender;
        var treeItem = (TreeItem)treeView.SelectedItem;
        if (treeItem.AssetType == ETreeItemType.Folder) return;

        await AppVM.MainVM.SetupMeshSelection(treeItem.FullPath);
    }

    private async void AssetFlatView_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        var listBox = (ListBox) sender;
        var selectedItem = (AssetItem) listBox.SelectedItem;
        if (selectedItem is null) return;
        
        await AppVM.MainVM.SetupMeshSelection(listBox.SelectedItems.OfType<AssetItem>().ToArray());
    }

    private void OnAssetDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var listBox = (ListBox)sender;
        var selectedItem = (AssetItem)listBox.SelectedItem;
        JumpToAsset(selectedItem.PathWithoutExtension);
    }

    private void OnPauseSwitch(object sender, RoutedEventArgs e)
    {
        if (AppVM.AssetHandlerVM is null) return;
        var toggleSwitch = (ToggleButton) sender;
        var assetType = (EAssetType) AssetControls.SelectedIndex;

        if (AppVM.AssetHandlerVM.Handlers.TryGetValue(assetType, out var handler))
        {
            handler.PauseState.IsPaused = toggleSwitch.IsChecked.HasValue && toggleSwitch.IsChecked.Value; 
        }
    }
}