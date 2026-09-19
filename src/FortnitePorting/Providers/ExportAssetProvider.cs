using System.Collections.Generic;
using System.IO;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Exporting.Providers;
using FortnitePorting.Services;

namespace FortnitePorting.Providers;

public class ExportAssetProvider(CUE4ParseService ueParse, DependencyService dependencies) : IExportAssetProvider, IService
{
    public AbstractVfsFileProvider Provider => ueParse.Provider!;

    public List<UAnimMontage> MaleLobbyMontages => ueParse.MaleLobbyMontages;
    public List<UAnimMontage> FemaleLobbyMontages => ueParse.FemaleLobbyMontages;

    public Dictionary<int, FColor> BeanstalkColors => ueParse.BeanstalkColors;
    public Dictionary<int, FLinearColor> BeanstalkMaterialProps => ueParse.BeanstalkMaterialProps;
    public Dictionary<int, FVector> BeanstalkAtlasTextureUVs => ueParse.BeanstalkAtlasTextureUVs;

    public FileInfo BinkaDecoderFile => dependencies.BinkaDecoderFile;
    public FileInfo RadaDecoderFile => dependencies.RadaDecoderFile;
    public FileInfo VgmStreamFile => dependencies.VgmStreamFile;
}
