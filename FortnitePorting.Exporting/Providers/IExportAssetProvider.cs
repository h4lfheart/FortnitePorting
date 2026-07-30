using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.FileProvider.Vfs;

namespace FortnitePorting.Exporting.Providers;

public interface IExportAssetProvider
{
    AbstractVfsFileProvider Provider { get; }

    List<UAnimMontage> MaleLobbyMontages { get; }
    List<UAnimMontage> FemaleLobbyMontages { get; }

    Dictionary<int, FColor> BeanstalkColors { get; }
    Dictionary<int, FLinearColor> BeanstalkMaterialProps { get; }
    Dictionary<int, FVector> BeanstalkAtlasTextureUVs { get; }

    FileInfo BinkaDecoderFile { get; }
    FileInfo RadaDecoderFile { get; }
    FileInfo VgmStreamFile { get; }
}
