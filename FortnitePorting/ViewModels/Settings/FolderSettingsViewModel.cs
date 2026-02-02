using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.Nanite;

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
            NaniteMeshFormat = ExportNanite ? ENaniteMeshFormat.NaniteSeparateFile : ENaniteMeshFormat.OnlyNormalLODs,
            CompressionFormat = CompressionFormat,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}