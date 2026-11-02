using CUE4Parse_Conversion.Options;

namespace FortnitePorting.Shared.Options;

public static class SharedExportOptions
{
    public static ExportOptions Default { get; } = Create();

    public static ExportOptions Create(
        EMeshQuality meshQuality = EMeshQuality.Highest,
        ENaniteMeshFormat naniteMeshFormat = ENaniteMeshFormat.NoNanite,
        bool exportMorphTargets = false)
        => new(
            meshQuality: meshQuality,
            naniteMeshFormat: naniteMeshFormat,
            exportMorphTargets: exportMorphTargets,
            exportMaterials: false);
}
