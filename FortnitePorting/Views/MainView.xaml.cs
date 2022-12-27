using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        
        if (DateTime.Now >= AppSettings.Current.LastUpdateAskTime.AddDays(1))
        {
            UpdateService.Start(automaticCheck: true);
            AppSettings.Current.LastUpdateAskTime = DateTime.Now;
        }

        if (AppSettings.Current.JustUpdated && !UpdateService.IsUpdateAvailable())
        {
            AppHelper.OpenWindow<PluginUpdateView>();
            AppSettings.Current.JustUpdated = false;
        }
        
        await AppVM.MainVM.Initialize();
    }

    private async void OnAssetTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl) return;
        if (AppVM.AssetHandlerVM is null) return;
        
        var assetType = (EAssetType) tabControl.SelectedIndex;

        if (AppVM.MainVM.CurrentAssetType == assetType) return;
        var handlers = AppVM.AssetHandlerVM.Handlers;
        foreach (var (handlerType, handlerData) in handlers)
        {
            if (handlerType == assetType)
            {
                handlerData.PauseState.Unpause();
            }
            else
            {
                handlerData.PauseState.Pause();
            }
        }
        
        if (!handlers[assetType].HasStarted)
        {
            await handlers[assetType].Execute();
        }
        
        DiscordService.Update(assetType);
        AppVM.MainVM.CurrentAssetType = assetType;
        AppVM.MainVM.ExtendedAssets.Clear();
    }

    private void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (AssetSelectorItem) listBox.SelectedItem;
        
        AppVM.MainVM.Styles.Clear();
        if (selected.Type == EAssetType.Prop)
        {
            AppVM.MainVM.TabModeText = "SELECTED ASSETS";
            if (listBox.SelectedItems.Count == 0) return;
            AppVM.MainVM.CurrentAsset = selected;
            AppVM.MainVM.ExtendedAssets.Clear();
            AppVM.MainVM.ExtendedAssets = listBox.SelectedItems.OfType<AssetSelectorItem>().ToList();
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
        var searchBox = (TextBox) sender;
        foreach (var tab in AssetControls.Items.OfType<TabItem>())
        {
            var listBox = (ListBox) tab.Content;
            listBox.Items.Filter = o => ((AssetSelectorItem) o).Match(searchBox.Text);
            listBox.Items.Refresh();
        }
    }

    private void OnSortSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var tab in AssetControls.Items.OfType<TabItem>())
        {
            var listBox = (ListBox) tab.Content;
            listBox.Items.SortDescriptions.Clear();
            listBox.Items.SortDescriptions.Add(new SortDescription("IsRandom", ListSortDirection.Descending));
            switch (AppVM.MainVM.SortType)
            {
                case ESortType.Default:
                    listBox.Items.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Ascending));
                    break;
                case ESortType.AZ:
                    listBox.Items.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
                    break;
                case ESortType.Season:
                    listBox.Items.SortDescriptions.Add(new SortDescription("SeasonNumber", ListSortDirection.Ascending));
                    listBox.Items.SortDescriptions.Add(new SortDescription("Rarity", ListSortDirection.Ascending));
                    break;
                case ESortType.Rarity:
                    listBox.Items.SortDescriptions.Add(new SortDescription("Rarity", ListSortDirection.Ascending));
                    listBox.Items.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Ascending));
                    break;
                case ESortType.Series:
                    listBox.Items.SortDescriptions.Add(new SortDescription("Series", ListSortDirection.Descending));
                    listBox.Items.SortDescriptions.Add(new SortDescription("Rarity", ListSortDirection.Descending));
                    break;
            }
        }
    }
}