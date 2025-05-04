using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls.Shapes;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.Fortnite;
using Serilog;
using Path = System.IO.Path;

namespace FortnitePorting.Export.Types;

public class MeshExport : BaseExport
{
    public readonly List<ExportMesh> Meshes = [];
    public readonly List<ExportMesh> OverrideMeshes = [];
    public readonly List<ExportOverrideMaterial> OverrideMaterials = [];
    public readonly List<ExportOverrideParameters> OverrideParameters = [];
    public ExportLightCollection Lights = new();
    public AnimExport? Animation;
    
    public MeshExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        var objectStyles = styles.OfType<ObjectStyleData>().ToArray();
        if (objectStyles.Length > 0)
        {
            foreach (var objectStyle in objectStyles)
            {
                Export(objectStyle.StyleData, exportType);
            }
            
            return;
        }

        Export(asset, exportType);
        
        var assetStyles = styles.OfType<AssetStyleData>();
        ExportStyles(asset, assetStyles);
    }

    public MeshExport(CustomAsset customAsset, EExportType exportType, ExportDataMeta metaData) : base(customAsset.Name, exportType, metaData)
    {
        string ExportCustom(string path)
        {
            var stream = Avalonia.Platform.AssetLoader.Open(new Uri($"avares://FortnitePorting/{path}"));
            
            var outPathPortion = path.SubstringAfter("Assets/");
            var outPath = Path.Combine(metaData.AssetsRoot, outPathPortion);
            Directory.CreateDirectory(outPath.SubstringBeforeLast("/"));
            File.WriteAllBytes(outPath, stream.ReadToEnd());

            return outPathPortion;
        }
        
        var mesh = customAsset.Mesh;

        var exportMesh = new ExportMesh();
        exportMesh.Name = customAsset.Name;
        exportMesh.Path = ExportCustom(mesh.Path);

        for (var matIndex = 0; matIndex < mesh.Materials.Length; matIndex++)
        {
            var material = mesh.Materials[matIndex];
            
            var exportMaterial = new ExportMaterial();
            exportMaterial.Name = material.Name;
            exportMaterial.Slot = matIndex;
            exportMaterial.Hash = material.Name.GetHashCode();

            foreach (var texture in material.Textures)
            {
                exportMaterial.Textures.Add(new TextureParameter(texture.Slot, ExportCustom(texture.Path), true,
                    TextureCompressionSettings.TC_Default));
            }
            
            exportMesh.Materials.Add(exportMaterial);
        }
        
        Meshes.Add(exportMesh);
        
    }

    public void Export(UObject asset, EExportType exportType)
    {
        switch (exportType)
        {
            case EExportType.Outfit:
            {
                UAnimMontage? montage = asset.GetOrDefault<UAnimMontage?>("FrontendAnimMontageIdleOverride");
                var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                if (asset.TryGetValue(out UObject heroDefinition, "HeroDefinition"))
                {
                    if (parts.Length == 0 && heroDefinition.TryGetValue(out UObject[] specializations, "Specializations"))
                    {
                        parts = specializations.First().GetOrDefault("CharacterParts", Array.Empty<UObject>());
                    }

                    montage ??= heroDefinition.GetOrDefault<UAnimMontage?>("FrontendAnimMontageIdleOverride");
                }
                
                foreach (var part in parts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                    
                    montage ??= part.GetOrDefault<UAnimMontage?>("FrontendAnimMontageIdleOverride");
                }

                if (Meshes.FirstOrDefault(mesh => mesh is ExportPart { Type: EFortCustomPartType.Body }) is ExportPart bodyPart)
                {
                    montage ??= bodyPart.GenderPermitted switch
                    {
                        EFortCustomGender.Female => UEParse.FemaleLobbyMontages.Random()!,
                        _ => UEParse.MaleLobbyMontages.Random()!
                    };
                }
                
                if (Exporter.Meta.Settings.ImportLobbyPoses && montage is not null)
                {
                    Animation = new AnimExport(montage.Name, montage, [], EExportType.Animation, Exporter.Meta);
                }
                
                break;
            }
            case EExportType.CharacterPart:
            {
                Meshes.AddIfNotNull(Exporter.CharacterPart(asset));
                
                break;
            }
            case EExportType.Backpack:
            {
                var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                foreach (var part in parts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                }
                
                break;
            }
            case EExportType.Pickaxe:
            {
                var weapon = asset.GetOrDefault<UObject?>("WeaponDefinition");
                if (weapon is null) break;

                Meshes.AddRange(Exporter.WeaponDefinition(weapon));
                break;
            }
            case EExportType.Glider:
            {
                var mesh = asset.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                if (mesh is null) break;

                var part = Exporter.Mesh(mesh);
                if (part is null) break;

                var overrideMaterials = asset.GetOrDefault("MaterialOverrides", Array.Empty<FStructFallback>());
                foreach (var overrideMaterial in overrideMaterials)
                {
                    part.OverrideMaterials.AddIfNotNull(Exporter.OverrideMaterial(overrideMaterial));
                }

                Meshes.Add(part);
                break;
            }
            case EExportType.Pet:
            {
                // backpack meshes
                var parts = asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());
                foreach (var part in parts) Meshes.AddIfNotNull(Exporter.CharacterPart(part));

                // pet mesh
                var petAsset = asset.Get<UObject>("DefaultPet");
                var prefabClassPath = petAsset.Get<FSoftObjectPath>("PetPrefabClass");
                var prefabExports = UEParse.Provider.LoadAllObjects(prefabClassPath.AssetPathName.Text.SubstringBeforeLast("."));
                if (prefabExports.FirstOrDefault(export => export.Name.Equals("PetMesh0")) is not USkeletalMeshComponentBudgeted meshComponent) break;
                
                var mesh = meshComponent.GetSkeletalMesh().Load<USkeletalMesh>();
                if (mesh is null) break;

                var exportMesh = Exporter.Mesh<ExportPart>(mesh);
                if (exportMesh is null) break;
                
                var meta = new ExportPoseAssetMeta();
                if (meshComponent.TryGetValue(out UAnimBlueprintGeneratedClass animBlueprint, "AnimClass"))
                {
                    var animBlueprintData = animBlueprint.ClassDefaultObject.Load()!;
                    if (animBlueprintData.TryGetValue(out UPoseAsset poseAsset, "FacePoseAsset"))
                    {
                        meta.PoseAsset = Exporter.Export(poseAsset); // most pets have empty pose assets now but whatever
                    }
                }

                exportMesh.Meta = meta;
                
                Meshes.AddIfNotNull(exportMesh);

                break;
            }
            case EExportType.Toy:
            {
                var actor = asset.Get<UBlueprintGeneratedClass>("ToyActorClass");

                var exportComponent = GetComponent(actor);
                exportComponent ??= GetComponent(actor.SuperStruct.Load<UBlueprintGeneratedClass>());
                if (exportComponent is null) break;

                Meshes.AddIfNotNull(Exporter.MeshComponent(exportComponent));
                break;

                UStaticMeshComponent? GetComponent(UBlueprintGeneratedClass? blueprint)
                {
                    if (blueprint is null) return null;
                    if (!blueprint.TryGetValue(out UObject internalComponentHandler, "InheritableComponentHandler")) return null;

                    var records = internalComponentHandler.GetOrDefault("Records", Array.Empty<FStructFallback>());
                    foreach (var record in records)
                    {
                        var component = record.Get<UObject>("ComponentTemplate");
                        if (component is not UStaticMeshComponent staticMeshComponent) continue;

                        return staticMeshComponent;
                    }

                    return null;
                }
            }
            case EExportType.Prop:
            {
                var levelSaveRecord = asset.Get<ULevelSaveRecord>("ActorSaveRecord");
                AddObjects(Exporter.LevelSaveRecord(levelSaveRecord));
                break;
            }
            case EExportType.Prefab:
            {
                if (asset.TryGetValue(out ULevelSaveRecord baseSaveRecord, "LevelSaveRecord"))
                {
                    throw new NotSupportedException("Legacy level save record prefabs are not supported.");
                }
                
                var recordCollectionLazy = asset.GetOrDefault<FPackageIndex?>("PlaysetPropLevelSaveRecordCollection");
                if (recordCollectionLazy is null || recordCollectionLazy.IsNull || !recordCollectionLazy.TryLoad(out var recordCollection) || recordCollection is null) break;

                var props = recordCollection.GetOrDefault<FStructFallback[]>("Items");
                var totalProps = props.Length;
                var currentProp = 0;
                foreach (var prop in props)
                {
                    currentProp++;
                    
                    var levelSaveRecord = prop.GetOrDefault<UObject?>("LevelSaveRecord");
                    if (levelSaveRecord is null) continue;

                    var actorSaveRecord = levelSaveRecord.Get<ULevelSaveRecord>("ActorSaveRecord");
                    var transform = prop.GetOrDefault<FTransform>("Transform");
                    var objects = Exporter.LevelSaveRecord(actorSaveRecord);
                    foreach (var mesh in objects)
                    {
                        mesh.Location += transform.Translation;
                        mesh.Rotation += transform.Rotator();
                        mesh.Scale *= transform.Scale3D;
                    }
                    
                    Exporter.Meta.OnUpdateProgress(objects.FirstOrDefault()?.Name ?? "Prop", currentProp, totalProps);

                    AddObjects(objects);
                }

                break;
            }
            case EExportType.Mesh:
            case EExportType.WeaponMod:
            {
                if (asset is UBlueprintGeneratedClass blueprintGeneratedClass)
                {
                    AddObjects(Exporter.Blueprint(blueprintGeneratedClass));
                }
                else
                {
                    Meshes.AddIfNotNull(Exporter.Mesh(asset));
                }
                break;
            }
            case EExportType.World:
            {
                if (asset is not UWorld world) break;

                Name = world.Owner?.Name.SubstringAfterLast("/") ?? world.Name;
                Meshes.AddRange(Exporter.World(world));
                break;
            }
            case EExportType.Item:
            {
                Meshes.AddRange(Exporter.WeaponDefinition(asset));
                break;
            }
            case EExportType.Resource:
            {
                Meshes.AddRange(Exporter.WeaponDefinition(asset));
                break;
            }
            case EExportType.Trap:
            {
                var actor = asset.Get<UBlueprintGeneratedClass>("BlueprintClass").ClassDefaultObject.Load();
                if (actor is null) break;

                var staticMesh = actor.GetOrDefault<UBaseBuildingStaticMeshComponent?>("StaticMeshComponent");
                if (staticMesh is not null)
                {
                    Meshes.AddIfNotNull(Exporter.MeshComponent(staticMesh));
                }

                var components = UEParse.Provider.LoadAllObjects(actor.GetPathName().SubstringBeforeLast("."));
                foreach (var component in components)
                {
                    if (component.Name.Equals(staticMesh?.Name)) continue;
                    Meshes.AddIfNotNull(Exporter.MeshComponent(component));
                }

                break;
            }
            case EExportType.FallGuysOutfit:
            {
                var parts = asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
                foreach (var part in parts)
                {
                    Meshes.AddIfNotNull(Exporter.CharacterPart(part));
                }

                var parameterSet = new ExportOverrideParameters();
                parameterSet.MaterialNameToAlter = "Global";
                var additionalFields = asset.GetOrDefault("AdditionalDataFields", Array.Empty<FPackageIndex>());
                foreach (var additionalField in additionalFields)
                {
                    var field = additionalField.Load();
                    if (field is null) continue;
                    if (!field.ExportType.Equals("BeanCosmeticItemDefinitionBase")) continue;

                    void Texture(string propertyName, string shaderName)
                    {
                        if (!field.TryGetValue(out UTexture2D texture, propertyName)) return;
                        
                        parameterSet.Textures.AddUnique(new TextureParameter(shaderName, 
                            Exporter.Export(texture), texture.SRGB, texture.CompressionSettings));
                    }
                    
                    void ColorIndex(string propertyName, string shaderName)
                    {
                        if (!field.TryGetValue(out int index, propertyName)) return;

                        var color = UEParse.BeanstalkColors[index];
                        parameterSet.Vectors.Add(new VectorParameter(shaderName, color.ToLinearColor()));
                    }
                    
                    void MaterialTypeIndex(string propertyName, string shaderName)
                    {
                        if (!field.TryGetValue(out int index, propertyName)) return;

                        var color = UEParse.BeanstalkMaterialProps[index];
                        parameterSet.Vectors.Add(new VectorParameter(shaderName, color));
                    }
                    
                    void AtlasTextureSlotIndex(string propertyName, string shaderName)
                    {
                        if (!field.TryGetValue(out int index, propertyName))
                        {
                            parameterSet.Vectors.Add(new VectorParameter(shaderName, new FLinearColor(0, 0.5f, 0, 0)));
                            return;
                        }

                        // do we know why this is inverted? no
                        // do we care? probably not
                        index = index switch
                        {
                            1 => 4,
                            2 => 3,
                            3 => 2,
                            4 => 1
                        };

                        var offset = UEParse.BeanstalkAtlasTextureUVs[index];
                        parameterSet.Vectors.Add(new VectorParameter(shaderName, new FLinearColor(offset.X, offset.Y, offset.Z, 0)));
                    }

                    // Eye
                    ColorIndex("BodyEyesColorIndex", "Body_EyesColor");
                    MaterialTypeIndex("BodyEyesMaterialTypeIndex", "Body_Eyes_MaterialProps");
                    
                    // Main
                    ColorIndex("BodyMainColorIndex", "Body_MainColor");
                    MaterialTypeIndex("BodyMainMaterialTypeIndex", "Body_MaterialProps");
                    
                    // Pattern
                    Texture("Body_Pattern", "Body_Pattern");
                    ColorIndex("BodySecondaryColorIndex", "Body_SecondaryColor");
                    MaterialTypeIndex("BodySecondaryMaterialTypeIndex", "Body_Secondary_MaterialProps");
                    
                    // Face Plate
                    ColorIndex("BodyFaceplateColorIndex", "Body_FacePlateColor");
                    MaterialTypeIndex("BodyFaceplateMaterialTypeIndex", "Body_Faceplate_MaterialProps");
                    
                    // Face Items
                    ColorIndex("EyelashesColorIndex", "Eyelashes_Color");
                    MaterialTypeIndex("EyelashesMaterialTypeIndex", "Eyelashes_MaterialProps");
                    ColorIndex("GlassesFrameColorIndex", "Glasses_Frame_Color");
                    MaterialTypeIndex("GlassesFrameMaterialTypeIndex", "Glasses_Frame_MaterialProps");
                    ColorIndex("GlassesLensesColorIndex", "Glasses_Lense_Color");
                    MaterialTypeIndex("GlassesLensesMaterialTypeIndex", "Glasses_Lense_MaterialProps");
                    
                    // Costume
                    ColorIndex("CostumeMainColorIndex", "Costume_MainColor");
                    MaterialTypeIndex("CostumeMainMaterialTypeIndex", "Costume_MainMaterialProps");
                    ColorIndex("CostumeSecondaryColorIndex", "Costume_Secondary_Color");
                    MaterialTypeIndex("CostumeSecondaryMaterialTypeIndex", "Costume_SecondaryMaterialProps");
                    ColorIndex("CostumeAccentColorIndex", "Costume_AccentColor");
                    MaterialTypeIndex("CostumeAccentMaterialTypeIndex", "Costume_AccentMaterialProps");
                    AtlasTextureSlotIndex("CostumePatternAtlasTextureSlot", "Costume_UVPatternPosition");
                    
                    
                    // Head Costume
                    ColorIndex("HeadCostumeMainColorIndex", "Head_Costume_MainColor");
                    MaterialTypeIndex("HeadCostumeMainMaterialTypeIndex", "Head_Costume_MainMaterialProps");
                    ColorIndex("HeadCostumeSecondaryColorIndex", "Head_Costume_Secondary_Color");
                    MaterialTypeIndex("HeadCostumeSecondaryMaterialTypeIndex", "Head_Costume_SecondaryMaterialProps");
                    ColorIndex("HeadCostumeAccentColorIndex", "Head_Costume_AccentColor");
                    MaterialTypeIndex("HeadCostumeAccentMaterialTypeIndex", "Head_Costume_AccentMaterialProps");
                    AtlasTextureSlotIndex("HeadCostumePatternAtlasTextureSlot", "Head_Costume_UVPatternPosition");

                    parameterSet.Vectors.Add(new VectorParameter("Body_GlassesEyeLashes", new FLinearColor
                    {
                        R = field.GetOrDefault<bool>("bGlasses") ? 1 : 0,
                        G = field.GetOrDefault<bool>("bGlassesLenses") ? 1 : 0,
                        B = field.GetOrDefault<bool>("bEyelashes") ? 1 : 0
                    }));
                }

                parameterSet.Hash = parameterSet.GetHashCode();
                
                OverrideParameters.Add(parameterSet);
                
                break;
            }
            case EExportType.FestivalBass:
            case EExportType.FestivalDrum:
            case EExportType.FestivalGuitar:
            case EExportType.FestivalKeytar:
            case EExportType.FestivalMic:
            {
                if (asset.TryGetValue(out USkeletalMesh mesh, "Mesh"))
                {
                    var exportMesh = Exporter.Mesh(mesh);

                    var material = asset.GetOrDefault<UMaterialInterface>("Material");
                    exportMesh?.Materials.AddIfNotNull(Exporter.Material(material, 0));

                    Meshes.AddIfNotNull(exportMesh);
                }

                if (asset.TryGetValue(out USkeletalMesh leftHandMesh, "LeftHandMesh"))
                {
                    var exportMesh = Exporter.Mesh(leftHandMesh);

                    var material = asset.GetOrDefault<UMaterialInterface>("LeftHandMaterial");
                    exportMesh?.Materials.AddIfNotNull(Exporter.Material(material, 0));

                    Meshes.AddIfNotNull(exportMesh);
                }

                if (asset.TryGetValue(out USkeletalMesh auxiliaryMesh, "AuxiliaryMesh"))
                {
                    var exportMesh = Exporter.Mesh(auxiliaryMesh);
                    if (exportMesh is null) break;

                    if (asset.TryGetValue(out UMaterialInterface auxMaterial, "AuxiliaryMaterial"))
                        exportMesh?.Materials.AddIfNotNull(Exporter.Material(auxMaterial, 0));

                    if (asset.TryGetValue(out UMaterialInterface auxMaterial2, "AuxiliaryMaterial2"))
                        exportMesh?.Materials.AddIfNotNull(Exporter.Material(auxMaterial2, 1));

                    Meshes.AddIfNotNull(exportMesh);
                }


                break;
            }
            case EExportType.Wildlife:
            {
                Meshes.AddIfNotNull(Exporter.Mesh(asset));
                break;
            }
            case EExportType.Vehicle:
            {
                var blueprint = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass");
                AddObjects(Exporter.Blueprint(blueprint));
                
                break;
            }
        }
    }

    private void AddObjects(IEnumerable<ExportObject> objects)
    {
        foreach (var obj in objects)
        {
            if (obj is ExportMesh exportMesh)
            {
                Meshes.Add(exportMesh);
            }
            else if (obj is ExportLight exportLight)
            {
                Lights.Add(exportLight);
            }
        }
    }
    
    private void ExportStyles(UObject asset, IEnumerable<AssetStyleData> styles)
    {
        var metaTagsToApply = new List<FGameplayTag>();
        var metaTagsToRemove = new List<FGameplayTag>();

        var styleDatas = styles.Select(data => data.StyleData).ToArray();
        foreach (var style in styleDatas)
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

        foreach (var style in styleDatas) ExportStyleData(style);
    }

    private void ExportStyleData(FStructFallback style)
    {
        var variantParts = style.GetOrDefault("VariantParts", Array.Empty<UObject>());
        foreach (var part in variantParts) OverrideMeshes.AddIfNotNull(Exporter.CharacterPart(part));

        var variantMaterials = style.GetOrDefault("VariantMaterials", Array.Empty<FStructFallback>());
        foreach (var material in variantMaterials) OverrideMaterials.AddIfNotNull(Exporter.OverrideMaterialSwap(material));

        var variantParameters = style.GetOrDefault("VariantMaterialParams", Array.Empty<FStructFallback>());
        foreach (var parameters in variantParameters) OverrideParameters.AddIfNotNull(Exporter.OverrideParameters(parameters));
        
        var variantMeshes = style.GetOrDefault("VariantMeshes", Array.Empty<FStructFallback>());
        foreach (var mesh in variantMeshes)
        {
            var overrideMesh = mesh.GetOrDefault<USkeletalMesh>("OverrideMesh");
            OverrideMeshes.AddIfNotNull(Exporter.Mesh(overrideMesh));
        }
    }
}
