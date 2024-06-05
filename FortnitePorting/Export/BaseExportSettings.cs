using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.UEFormat.Enums;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Export;

public partial class BaseExportSettings : ViewModelBase
{
    [ObservableProperty] private EMeshFormat _meshFormat = EMeshFormat.UEFormat;
    [ObservableProperty] private EAnimFormat _animFormat = EAnimFormat.UEFormat;
    [ObservableProperty] private EFileCompressionFormat _compressionFormat = EFileCompressionFormat.ZSTD;
    [ObservableProperty] private EImageFormat _imageFormat = EImageFormat.PNG;

    [ObservableProperty] private bool _exportMaterials = true;
    
    public virtual ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions();
    }
}
