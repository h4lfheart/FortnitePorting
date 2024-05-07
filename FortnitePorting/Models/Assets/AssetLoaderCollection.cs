using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Models.Assets;

public partial class AssetLoaderCollection : ObservableObject
{
    public readonly List<AssetLoaderCategory> Categories =
    [
        new AssetLoaderCategory(EAssetCategory.Cosmetics)
        {
            Loaders = 
            [
                new AssetLoader(EAssetType.Outfit)
                {
                    DefaultSelected = true,
                    ClassNames = ["AthenaCharacterItemDefinition"],
                    PlaceholderIconPath = "FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Item_Outfit",
                    IconHandler = asset =>
                    {
                        asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
                        if (asset.TryGetValue(out UObject hero, "HeroDefinition")) 
                            hero.TryGetValue(out previewImage, "SmallPreviewImage", "LargePreviewImage");

                        return previewImage;
                    }
                }
            ]
        }

    ];
    
    [ObservableProperty] private ObservableCollection<NavigationViewItemBase> _navItems = [];
    [ObservableProperty] private NavigationViewItemBase _selectedNavItem;
    
    [ObservableProperty] private AssetLoader _activeLoader;
    [ObservableProperty] private ReadOnlyObservableCollection<AssetItem> _activeCollection;

    public AssetLoaderCollection()
    {
        TaskService.RunDispatcher(() =>
        {
            foreach (var category in Categories)
            {
                NavItems.Add(new NavigationViewItem
                {
                    Content = category.Category.GetDescription(),
                    SelectsOnInvoked = false,
                    IconSource = new ImageIconSource
                    {
                        Source = ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{category.Category.ToString()}.png")
                    },
                    MenuItemsSource = category.Loaders.Select(loader => new NavigationViewItem
                    {
                        Tag = loader.Type, 
                        Content = loader.Type.GetDescription(), 
                        IconSource = new ImageIconSource
                        {
                            Source = ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{loader.Type.ToString()}.png")
                        }
                    })
                });
            }
        });
    }
    
    public async Task Load(EAssetType type)
    {
        Set(type);
        await ActiveLoader.Load();
    }
    
    public void Set(EAssetType type)
    {
        ActiveLoader = Get(type);
        ActiveCollection = ActiveLoader.Filtered;
    }

    public AssetLoader Get(EAssetType type)
    {
        foreach (var category in Categories)
        {
            if (category.Loaders.FirstOrDefault(loader => loader.Type == type) is { } assetLoader)
            {
                return assetLoader;
            }
        }

        return null!; // if this happens it's bc im stupid
    }
}