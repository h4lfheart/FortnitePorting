using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;

namespace FortnitePorting.ViewModels.Settings;

public partial class FolderSettingsViewModel : BaseExportSettings
{
    [ObservableProperty] private ELodFormat _lodFormat = ELodFormat.AllLods;
    
    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = LodFormat,
            MeshFormat = MeshFormat,
            AnimFormat = AnimFormat,
            CompressionFormat = CompressionFormat,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}