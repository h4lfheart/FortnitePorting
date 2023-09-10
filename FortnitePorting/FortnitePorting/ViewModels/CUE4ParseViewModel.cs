using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ViewModelBase
{
    public HybridFileProvider Provider;

    private static readonly VersionContainer LatestVersionContainer = new(EGame.GAME_UE5_3, optionOverrides: new Dictionary<string, bool>
    {
        { "SkeletalMesh.KeepMobileMinLODSettingOnDesktop", true },
        { "StaticMesh.KeepMobileMinLODSettingOnDesktop", true }
    });
    
    public CUE4ParseViewModel()
    {
        Provider = AppSettings.Current.LoadingType switch
        {
            ELoadingType.Local => new HybridFileProvider(AppSettings.Current.LocalArchivePath, LatestVersionContainer),
            ELoadingType.Live => new HybridFileProvider(LatestVersionContainer),
            ELoadingType.Custom => new HybridFileProvider(AppSettings.Current.CustomArchivePath, new VersionContainer(AppSettings.Current.CustomUnrealVersion))
        };
    }

    public override async Task Initialize()
    {
        LoadingVM.LoadingTiers = 1;
        
        LoadingVM.Update("Loading Archive");
        await InitializeProvider();
        
    }
    
    private async Task InitializeProvider()
    {
        switch (AppSettings.Current.LoadingType)
        {
            case ELoadingType.Local:
            case ELoadingType.Custom:
            {
                Provider.InitializeLocal();
                break;
            }
            case ELoadingType.Live: // TODO
            {
                break;
            }
        }
    }
    
    
}