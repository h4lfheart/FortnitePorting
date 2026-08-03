using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.UE4.Assets.Exports.Nanite;

namespace FortnitePorting.Exporting.Models;

public class ExportSettings
{
    public EFileCompressionFormat CompressionFormat { get; set; } = EFileCompressionFormat.ZSTD;
    public EImageFormat ImageFormat { get; set; } = EImageFormat.PNG;
    public bool ExportMaterials { get; set; } = true;
    public bool ExportMaterialGraph { get; set; } = false;
    public EMeshFormat MeshFormat { get; set; } = EMeshFormat.UEFormat;
    public bool ExportNanite { get; set; } = false;
    public bool ImportInstancedFoliage { get; set; } = true;
    public EAnimFormat AnimFormat { get; set; } = EAnimFormat.UEFormat;
    public bool ImportLobbyPoses { get; set; } = false;
    public ESoundFormat SoundFormat { get; set; } = ESoundFormat.WAV;
    public bool OpenFoldersOnExport { get; set; } = false;

    public ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            MeshFormat = MeshFormat,
            AnimFormat = AnimFormat,
            CompressionFormat = CompressionFormat,
            NaniteMeshFormat = ExportNanite ? ENaniteMeshFormat.NaniteSeparateFile : ENaniteMeshFormat.OnlyNormalLODs
        };
    }
}
