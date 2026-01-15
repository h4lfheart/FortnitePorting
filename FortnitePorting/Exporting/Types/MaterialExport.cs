using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Types;

public class MaterialExport : BaseExport
{
    public readonly List<ExportMaterial> Materials = [];
    
    public MaterialExport(string name, UObject asset, EExportType exportType, ExportDataMeta metaData) : base(name, exportType, metaData)
    {
        Materials.AddIfNotNull(Exporter.Material((UMaterialInterface)asset, 0));
    }
}