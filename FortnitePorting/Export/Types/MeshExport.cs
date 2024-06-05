using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Export.Models;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Export.Types;

public class MeshExport : BaseExport
{
    public readonly List<ExportMesh> Meshes = [];
    public readonly List<ExportMesh> OverrideMeshes = [];
    
    public MeshExport(string name, UObject asset, EExportType exportType, ExportMetaData metaData) : base(name, asset, exportType, metaData)
    {
        switch (exportType)
        {
            case EExportType.Outfit:
            {
                var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());

                foreach (var part in parts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                }
                
                break;
            }
        }
    }
}