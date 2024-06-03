using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Export.Models;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Export.Types;

public class MeshExport : BaseExport
{
    private readonly List<Model> Meshes = [];
    
    public MeshExport(string name, UObject asset, EExportType exportType) : base(name, asset, exportType)
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