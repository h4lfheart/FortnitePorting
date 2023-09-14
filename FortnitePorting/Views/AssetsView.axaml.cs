using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using FortnitePorting.Application;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class AssetsView : ViewBase<AssetsViewModel>
{
    public AssetsView()
    {
        InitializeComponent();
    }
    
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await TaskService.RunAsync(async () => await ViewModel.Initialize());
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not AssetItem asset) return;
        
        if (asset.IsRandom)
        {
            listBox.SelectedIndex = RandomGenerator.Next(1, listBox.Items.Count);
            return;
        }

        AssetsVM.CurrentAsset = asset;
    }

    private async void OnAssetTypeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggleButton) return;
        if (toggleButton.Tag is not EAssetType assetType) return;
        
        var buttons = AssetTypePanel.Children.OfType<ToggleButton>();
        foreach (var button in buttons)
        {
            if (button.Tag is not EAssetType buttonAssetType) continue;
            button.IsChecked = buttonAssetType == assetType;
        }
        
        if (AssetsVM.CurrentTabType == assetType) return;
        
        var assetLoader = AssetsVM.Get(assetType);
        AssetsVM.CurrentTabType = assetType;
        AssetsListBox.ItemsSource = assetLoader.Target;
        await assetLoader.Load();
        

        var loaders = AssetsVM.Loaders;
        foreach (var loader in loaders)
        {
            if (loader.Type == assetType)
            {
                loader.Pause.Unpause();
            }
            else
            {
                loader.Pause.Pause();
            }
        }
    }
}