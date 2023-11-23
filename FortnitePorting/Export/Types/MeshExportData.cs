using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;

namespace FortnitePorting.Export.Types;

public class MeshExportData : ExportDataBase
{
    public readonly List<ExportMesh> Meshes = new();
    public readonly List<ExportMesh> OverrideMeshes = new();
    public readonly List<ExportOverrideMaterial> OverrideMaterials = new();
    public readonly List<ExportOverrideParameters> OverrideParameters = new();

    public MeshExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportType exportType) : base(name, asset, styles, type, exportType)
    {
        switch (type)
        {
            case EAssetType.Outfit:
            {
                var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                AssetsVM.ExportChunks = parts.Length;
                foreach (var part in parts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                    AssetsVM.ExportProgress++;
                }
                break;
            }
            case EAssetType.Backpack:
            {
                var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                foreach (var part in parts) Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                break;
            }
            case EAssetType.Pickaxe:
            {
                var weapon = asset.GetOrDefault<UObject?>("WeaponDefinition");
                if (weapon is null) break;

                Meshes.AddRange(Exporter.WeaponDefinition(weapon));
                break;
            }
            case EAssetType.Glider:
            {
                var mesh = asset.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                if (mesh is null) break;

                var part = Exporter.Mesh(mesh);
                if (part is null) break;

                var overrideMaterials = asset.GetOrDefault("MaterialOverrides", Array.Empty<FStructFallback>());
                foreach (var overrideMaterial in overrideMaterials) part.OverrideMaterials.AddIfNotNull(Exporter.OverrideMaterial(overrideMaterial));

                Meshes.Add(part);
                break;
            }
            case EAssetType.Pet:
            {
                // backpack meshes
                var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                foreach (var part in parts) Meshes.AddIfNotNull(Exporter.CharacterPart(part));

                // pet mesh
                var petAsset = asset.Get<UObject>("DefaultPet");
                var blueprintPath = petAsset.Get<FSoftObjectPath>("PetPrefabClass");
                var blueprintExports = CUE4ParseVM.Provider.LoadAllObjects(blueprintPath.AssetPathName.Text.SubstringBeforeLast("."));
                var meshComponent = blueprintExports.FirstOrDefault(export => export.Name.Equals("PetMesh0")) as USkeletalMeshComponentBudgeted;
                var mesh = meshComponent?.GetSkeletalMesh().Load<USkeletalMesh>();
                if (mesh is not null) Meshes.AddIfNotNull(Exporter.Mesh(mesh));

                break;
            }
            case EAssetType.Toy:
                break;
            case EAssetType.Prop:
            {
                var levelSaveRecord = asset.Get<ULevelSaveRecord>("ActorSaveRecord");
                var meshes = Exporter.LevelSaveRecord(levelSaveRecord);
                Meshes.AddRange(meshes);
                break;
            }
            case EAssetType.Prefab:
            {
                var recordCollectionLazy = asset.GetOrDefault<FPackageIndex?>("PlaysetPropLevelSaveRecordCollection");
                if (recordCollectionLazy is null || recordCollectionLazy.IsNull || !recordCollectionLazy.TryLoad(out var recordCollection) || recordCollection is null) break;

                var props = recordCollection.GetOrDefault<FStructFallback[]>("Items");
                AssetsVM.ExportChunks = props.Length;
                foreach (var prop in props)
                {
                    var levelSaveRecord = prop.GetOrDefault<UObject?>("LevelSaveRecord");
                    if (levelSaveRecord is null) continue;

                    var actorSaveRecord = levelSaveRecord.Get<ULevelSaveRecord>("ActorSaveRecord");
                    var transform = prop.GetOrDefault<FTransform>("Transform");
                    var meshes = Exporter.LevelSaveRecord(actorSaveRecord);
                    foreach (var mesh in meshes)
                    {
                        mesh.Location += transform.Translation;
                        mesh.Rotation += transform.Rotator();
                        mesh.Scale *= transform.Scale3D;
                    }
                    Meshes.AddRange(meshes);
                    AssetsVM.ExportProgress++;
                }

                break;
            }
            case EAssetType.Item:
            {
                Meshes.AddRange(Exporter.WeaponDefinition(asset));
                break;
            }
            case EAssetType.Trap:
                break;
            case EAssetType.Vehicle:
                break;
            case EAssetType.Wildlife:
            {
                var wildlifeMesh = (USkeletalMesh) asset;
                Meshes.AddIfNotNull(Exporter.Mesh(wildlifeMesh));
                break;
            }
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

        ExportStyles(asset, styles);
    }

    private void ExportStyles(UObject asset, FStructFallback[] styles)
    {
        var metaTagsToApply = new List<FGameplayTag>();
        var metaTagsToRemove = new List<FGameplayTag>();
        foreach (var style in styles)
        {
            var tags = style.Get<FStructFallback>("MetaTags");

            var tagsToApply = tags.Get<FGameplayTagContainer>("MetaTagsToApply");
            metaTagsToApply.AddRange(tagsToApply.GameplayTags);

            var tagsToRemove = tags.Get<FGameplayTagContainer>("MetaTagsToRemove");
            metaTagsToRemove.AddRange(tagsToRemove.GameplayTags);
        }

        var metaTags = new FGameplayTagContainer(metaTagsToApply.Where(tag => !metaTagsToRemove.Contains(tag)).ToArray());
        var itemStyles = asset.GetOrDefault("ItemVariants", Array.Empty<UObject>());
        var tagDrivenStyles = itemStyles.Where(style => style.ExportType.Equals("FortCosmeticLoadoutTagDrivenVariant"));
        foreach (var tagDrivenStyle in tagDrivenStyles)
        {
            var options = tagDrivenStyle.Get<FStructFallback[]>("Variants");
            foreach (var option in options)
            {
                var requiredConditions = option.Get<FStructFallback[]>("RequiredConditions");
                foreach (var condition in requiredConditions)
                {
                    var metaTagQuery = condition.Get<FGameplayTagQuery>("MetaTagQuery");
                    if (metaTags.MatchesQuery(metaTagQuery)) ExportStyleData(option);
                }
            }
        }

        foreach (var style in styles) ExportStyleData(style);
    }

    private void ExportStyleData(FStructFallback style)
    {
        var variantParts = style.GetOrDefault("VariantParts", Array.Empty<UObject>());
        foreach (var part in variantParts) OverrideMeshes.AddIfNotNull(Exporter.CharacterPart(part));

        var variantMaterials = style.GetOrDefault("VariantMaterials", Array.Empty<FStructFallback>());
        foreach (var material in variantMaterials) OverrideMaterials.AddIfNotNull(Exporter.OverrideMaterial(material));
        
        var variantParameters = style.GetOrDefault("VariantMaterialParams", Array.Empty<FStructFallback>());
        foreach (var parameters in variantParameters) OverrideParameters.AddIfNotNull(Exporter.OverrideParameters(parameters));
    }
}