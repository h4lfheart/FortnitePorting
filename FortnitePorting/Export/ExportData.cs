using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace FortnitePorting.Export;

public class ExportData
{
    public string Name;
    public string Type;
    public List<string> Meshes = new();

    public static async Task<ExportData> Create(UObject asset, EAssetType assetType)
    {
        var data = new ExportData();
        data.Name = asset.GetOrDefault("DisplayName", new FText("Unknown")).Text;
        data.Type = assetType.ToString();

        switch (assetType)
        {
            case EAssetType.Outfit:
            {
                var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                foreach (var part in parts)
                {
                    data.Meshes.Add(await ExportHelpers.ExportObjectAsync(part.Get<USkeletalMesh>("SkeletalMesh")));
                }
                break;
            }

            case EAssetType.Backpack:
            {
                var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                foreach (var part in parts)
                {
                    data.Meshes.Add(await ExportHelpers.ExportObjectAsync(part.Get<USkeletalMesh>("SkeletalMesh")));
                }
                break;
            }
            case EAssetType.Pickaxe:
            {
                var weapon = asset.Get<UObject>("WeaponDefinition");
                var mesh = weapon.Get<USkeletalMesh>("WeaponMeshOverride");
                data.Meshes.Add(await ExportHelpers.ExportObjectAsync(mesh));
                break;
            }
            case EAssetType.Glider:
                data.Meshes.Add(await ExportHelpers.ExportObjectAsync(asset.Get<USkeletalMesh>("SkeletalMesh")));
                break;
            case EAssetType.Weapon:
            {
                if (!asset.TryGetValue<USkeletalMesh>(out var mesh, "WeaponMeshOverride"))
                {
                    asset.TryGetValue<USkeletalMesh>(out mesh, "PickupSkeletalMesh");
                }
                data.Meshes.Add(await ExportHelpers.ExportObjectAsync(mesh));
                break;
            }
            case EAssetType.Dance:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        
        await Task.WhenAll(ExportHelpers.RunningExporters);
        return data;
    }
}