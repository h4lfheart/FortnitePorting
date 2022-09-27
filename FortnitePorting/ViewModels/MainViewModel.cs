using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.AppUtils;
using FortnitePorting.Views;
using FortnitePorting.Views.Controls;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();

    public async Task Initialize()
    {
        AppVM.CUE4ParseVM = new CUE4ParseViewModel(AppSettings.Current.ArchivePath);
        await AppVM.CUE4ParseVM.Initialize();

        var sw = new Stopwatch();
        sw.Start();
        var characters = AppVM.CUE4ParseVM.AssetRegistry?.PreallocatedAssetDataBuffers.Where(x =>
            x.AssetClass.PlainText.Equals("AthenaCharacterItemDefinition"));
        foreach (var outfit in characters)
        {
            var asset = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(outfit.ObjectPath);
            
            asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage");
            if (asset.TryGetValue(out UObject heroDef, "HeroDefinition"))
            {
                heroDef.TryGetValue(out previewImage, "SmallPreviewImage");
            }
            
            if (previewImage is null) continue;
            
            await Application.Current.Dispatcher.InvokeAsync(() => outfits.Add(new AssetSelectorItem(asset, previewImage)), DispatcherPriority.Background);
        }
        sw.Stop();
        
        AppLog.Information($"Finished loading outfits in {Math.Round(sw.Elapsed.TotalSeconds, 3)}s");
       
    }
    
    [RelayCommand]
    public void Menu(string parameter)
    {
        switch (parameter)
        {
            case "Open_Assets":
                AppHelper.Launch(App.AssetsFolder.FullName);
                break;
            case "Open_Data":
                AppHelper.Launch(App.DataFolder.FullName);
                break;
            case "Open_Exports":
                AppHelper.Launch(App.ExportsFolder.FullName);
                break;
            case "File_Restart":
                AppVM.Restart();
                break;
            case "File_Quit":
                AppVM.Quit();
                break;
            case "Settings_Options":
                AppHelper.OpenWindow<SettingsView>();
                break;
            case "Settings_Startup":
                AppHelper.OpenWindow<StartupView>();
                break;
            case "Tools_BundleDownloader":
                AppHelper.OpenWindow<BundleDownloaderView>();
                break;
            case "Tools_Update":
                // TODO
                break;
            case "Help_Discord":
                AppHelper.Launch(Globals.DISCORD_URL);
                break;
            case "Help_GitHub":
                AppHelper.Launch(Globals.GITHUB_URL);
                break;
            case "Help_About":
                // TODO
                break;
        }
    }
}