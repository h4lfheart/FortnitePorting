using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.Extensions;

namespace FortnitePorting.Export.Types;

public class MeshExportData : ExportDataBase
{
    public readonly List<ExportMesh> Meshes = new();
    public MeshExportData(string name, UObject asset, EAssetType type, EExportType exportType) : base(name, asset, type, exportType)
    {
        switch (type)
        {
            case EAssetType.Outfit:
            {
                var characterParts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                foreach (var part in characterParts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                }
                break;
            }
            case EAssetType.Backpack:
                break;
            case EAssetType.Pickaxe:
                break;
            case EAssetType.Glider:
                break;
            case EAssetType.Pet:
                break;
            case EAssetType.Toy:
                break;
            case EAssetType.Spray:
                break;
            case EAssetType.Banner:
                break;
            case EAssetType.LoadingScreen:
                break;
            case EAssetType.Emote:
                break;
            case EAssetType.Prop:
                break;
            case EAssetType.Gallery:
                break;
            case EAssetType.Item:
                break;
            case EAssetType.Trap:
                break;
            case EAssetType.Vehicle:
                break;
            case EAssetType.Wildlife:
                break;
            case EAssetType.Mesh:
            {
                switch (asset)
                {
                    case USkeletalMesh skeletalMesh:
                        Meshes.AddIfNotNull(Exporter.Mesh(skeletalMesh));
                        break;
                    case UStaticMesh staticMesh:
                        Meshes.AddIfNotNull(Exporter.Mesh(staticMesh));
                        break;
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}