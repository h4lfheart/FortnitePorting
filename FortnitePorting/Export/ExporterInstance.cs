using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
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
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.ViewModels;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Export;

public class ExporterInstance
{
    public readonly List<Task> ExportTasks = new();
    private readonly HashSet<ExportMaterial> MaterialCache = new();
    private readonly ExportOptionsBase AppExportOptions;
    private readonly ExporterOptions FileExportOptions;

    private static readonly string[] OverridesToIgnore =
    {
        "M_Trap_InvalidAttachment"
    };

    public ExporterInstance(EExportType exportType)
    {
        AppExportOptions = AppSettings.Current.ExportOptions.Get(exportType);
        FileExportOptions = AppExportOptions.CreateExportOptions();
    }

    public ExportPart? CharacterPart(UObject part)
    {
        var skeletalMesh = part.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
        if (skeletalMesh is null) return null;

        var exportPart = Mesh<ExportPart>(skeletalMesh);
        if (exportPart is null) return null;

        if (part.TryGetValue(out FStructFallback[] materialOverrides, "MaterialOverrides"))
            foreach (var material in materialOverrides)
                exportPart.OverrideMaterials.AddIfNotNull(OverrideMaterial(material));

        exportPart.Type = part.GetOrDefault<EFortCustomPartType>("CharacterPartType").ToString();

        if (part.TryGetValue(out UObject additionalData, "AdditionalData"))
            switch (additionalData.ExportType)
            {
                case "CustomCharacterHeadData":
                {
                    var meta = new ExportHeadMeta();

                    foreach (var type in Enum.GetValues<ECustomHatType>())
                        if (additionalData.TryGetValue(out FName[] morphNames, type + "MorphTargets"))
                            meta.MorphNames[type] = morphNames.First().Text;

                    if (additionalData.TryGetValue(out UObject skinColorSwatch, "SkinColorSwatch"))
                    {
                        var colorPairs = skinColorSwatch.GetOrDefault("ColorPairs", Array.Empty<FStructFallback>());
                        var skinColorPair = colorPairs.FirstOrDefault(x => x.Get<FName>("ColorName").Text.Equals("Skin Boost Color and Exponent", StringComparison.OrdinalIgnoreCase));
                        if (skinColorPair is not null) meta.SkinColor = skinColorPair.Get<FLinearColor>("ColorValue");
                    }

                    if (additionalData.TryGetValue(out UAnimBlueprintGeneratedClass animBlueprint, "AnimClass"))
                    {
                        var animBlueprintData = animBlueprint.ClassDefaultObject.Load()!;
                        if (animBlueprintData.TryGetValue(out FStructFallback poseAssetNode, "AnimGraphNode_PoseBlendNode"))
                        {
                            var poseAsset = poseAssetNode.Get<UPoseAsset>("PoseAsset");
                            Log.Information(poseAsset.GetPathName());
                        }
                    }

                    exportPart.Meta = meta;
                    break;
                }
                case "CustomCharacterHatData":
                {
                    var meta = new ExportHatMeta
                    {
                        AttachToSocket = part.GetOrDefault("bAttachToSocket", true),
                        Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text
                    };

                    if (additionalData.TryGetValue(out FName hatType, "HatType")) meta.HatType = hatType.Text.Replace("ECustomHatType::ECustomHatType_", string.Empty);
                    exportPart.Meta = meta;
                    break;
                }
                case "CustomCharacterCharmData":
                {
                    var meta = new ExportAttachMeta
                    {
                        AttachToSocket = part.GetOrDefault("bAttachToSocket", true),
                        Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text
                    };
                    exportPart.Meta = meta;
                    break;
                }
            }

        return exportPart;
    }

    public List<ExportMesh> WeaponDefinition(UObject weaponDefinition)
    {
        var weaponMeshes = WeaponDefinitionMeshes(weaponDefinition);
        var exportWeapons = new List<ExportMesh>();
        foreach (var weaponMesh in weaponMeshes)
        {
            exportWeapons.AddIfNotNull(weaponMesh switch
            {
                USkeletalMesh skeletalMesh => Mesh(skeletalMesh),
                UStaticMesh staticMesh => Mesh(staticMesh),
                _ => null
            });
        }

        return exportWeapons;
    }

    // TODO REFACTOR TO BE BETTER
    public static List<UObject> WeaponDefinitionMeshes(UObject weaponDefinition)
    {
        var exportWeapons = new List<UObject>();

        var skeletalMesh = weaponDefinition.GetOrDefault<USkeletalMesh?>("WeaponMeshOverride");
        skeletalMesh ??= weaponDefinition.GetOrDefault<USkeletalMesh?>("PickupSkeletalMesh");
        if (skeletalMesh is not null) exportWeapons.AddIfNotNull(skeletalMesh);

        var offhandSkeletalMesh = weaponDefinition.GetOrDefault<USkeletalMesh?>("WeaponMeshOffhandOverride");
        if (offhandSkeletalMesh is not null) exportWeapons.AddIfNotNull(offhandSkeletalMesh);

        if (skeletalMesh is null)
        {
            var staticMesh = weaponDefinition.GetOrDefault<UStaticMesh?>("PickupStaticMesh");
            if (staticMesh is not null) exportWeapons.AddIfNotNull(staticMesh);
        }

        if (exportWeapons.Count == 0 && weaponDefinition.TryGetValue(out UBlueprintGeneratedClass weaponActorClass, "WeaponActorClass"))
        {
            var weaponActorData = weaponActorClass.ClassDefaultObject.Load()!;
            if (weaponActorData.TryGetValue(out UObject weaponMeshData, "WeaponMesh"))
            {
                var weaponMesh = weaponMeshData.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                if (weaponMesh is not null) exportWeapons.AddIfNotNull(weaponMesh);
            }

            if (weaponActorData.TryGetValue(out UObject leftWeaponMeshData, "LeftHandWeaponMesh"))
            {
                var leftWeaponMesh = leftWeaponMeshData.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
                if (leftWeaponMesh is not null) exportWeapons.AddIfNotNull(leftWeaponMesh);
            }
        }

        return exportWeapons;
    }

    public List<ExportMesh> LevelSaveRecord(ULevelSaveRecord levelSaveRecord)
    {
        var exportMeshes = new List<ExportMesh>();
        foreach (var (index, templateRecord) in levelSaveRecord.TemplateRecords)
        {
            var actor = templateRecord.ActorClass.Load<UBlueprintGeneratedClass>().ClassDefaultObject.Load();
            if (actor is null) continue;
            
            if (actor.TryGetValue(out UStaticMesh staticMesh, "StaticMesh"))
            {
                exportMeshes.AddIfNotNull(Mesh(staticMesh));
            }
            else
            {
                var components = CUE4ParseVM.Provider.LoadAllObjects(actor.GetPathName().SubstringBeforeLast("."));
                foreach (var component in components)
                {
                    exportMeshes.AddIfNotNull(component switch
                    {
                        UStaticMeshComponent staticMeshComponent => MeshComponent(staticMeshComponent),
                        USkeletalMeshComponent skeletalMeshComponent => MeshComponent(skeletalMeshComponent),
                        _ => null
                    });
                }
            }
            
            if (exportMeshes.Count == 0) continue;

            var targetMesh = exportMeshes.First();
            if (actor.TryGetValue(out UStaticMesh doorMesh, "DoorMesh"))
            {
                var doorOffset = actor.GetOrDefault<TIntVector3<float>>("DoorOffset").ToFVector();
                var doorRotation = actor.GetOrDefault("DoorRotationOffset", FRotator.ZeroRotator);
                doorRotation.Pitch *= -1;
                
                var exportDoorMesh = Mesh(doorMesh)!;
                exportDoorMesh.Location = doorOffset;
                exportDoorMesh.Rotation = doorRotation;
                targetMesh.Children.AddIfNotNull(exportDoorMesh);

                if (actor.GetOrDefault("bDoubleDoor", false))
                {
                    var exportDoubleDoorMesh = exportDoorMesh with
                    {
                        Location = exportDoorMesh.Location with { X = -exportDoorMesh.Location.X },
                        Scale = exportDoorMesh.Scale with { X = -exportDoorMesh.Scale.X }
                    };
                    targetMesh.Children.AddIfNotNull(exportDoubleDoorMesh);
                }
                else if (actor.TryGetValue(out UStaticMesh doubleDoorMesh, "DoubleDoorMesh"))
                {
                    var exportDoubleDoorMesh = Mesh(doubleDoorMesh)!;
                    exportDoubleDoorMesh.Location = doorOffset;
                    exportDoubleDoorMesh.Rotation = doorRotation;
                    targetMesh.Children.AddIfNotNull(exportDoubleDoorMesh);
                }
                
            }

            var textureDatas = new Dictionary<int, UBuildingTextureData>();
            var actorData = levelSaveRecord.ActorData[index];
            if (actorData.TryGetAllValues(out string?[] textureDataRawPaths, "TextureData"))
            {
                for (var i = 0; i < textureDataRawPaths.Length; i++)
                {
                    var textureDataPath = textureDataRawPaths[i];
                    if (textureDataPath is null || string.IsNullOrEmpty(textureDataPath)) continue;
                    if (!CUE4ParseVM.Provider.TryLoadObject(textureDataPath, out UBuildingTextureData textureData)) continue;
                    textureDatas.Add(i, textureData);
                }
            }
            else
            {
                var textureDataPaths = templateRecord.ActorDataReferenceTable;
                for (var i = 0; i < textureDataPaths.Length; i++)
                {
                    var textureDataPath = textureDataPaths[i];
                    if (textureDataPath.AssetPathName.IsNone || string.IsNullOrEmpty(textureDataPath.AssetPathName.Text)) continue;
                    var textureData = textureDataPath.Load<UBuildingTextureData>();
                    textureDatas.Add(i, textureData);
                }
            }
            
            // reminder that texturedata is the worst thing ever to be created WHY WONT IT WORK
            foreach (var (textureDataIndex, textureData) in textureDatas)
            {
                targetMesh.TextureData.AddIfNotNull(TextureData(textureData, index));
            }
        }

        return exportMeshes;
    }

    public ExportTextureData? TextureData(UBuildingTextureData? textureData, int index = 0)
    {
        if (textureData is null) return null;
        
        var exportTextureData = new ExportTextureData();

        var textureSuffix = index > 0 ? $"_Texture_{index + 1}" : string.Empty;
        var specSuffix = index > 0 ? $"_{index + 1}" : string.Empty;
        exportTextureData.Diffuse = AddData(textureData.Diffuse, "Diffuse", textureSuffix);
        exportTextureData.Normal = AddData(textureData.Normal, "Normals", textureSuffix);
        exportTextureData.Specular = AddData(textureData.Specular, "SpecularMasks", specSuffix);
        exportTextureData.Hash = textureData.GetPathName().GetHashCode();
        return exportTextureData;
        
        TextureParameter? AddData(UTexture? texture, string prefix, string suffix)
        {
            return texture is null ? default : new TextureParameter(prefix + suffix, Export(texture), texture.SRGB, texture.CompressionSettings);
        }
    }

    public ExportMesh? MeshComponent(USkeletalMeshComponent meshComponent)
    {
        var mesh = meshComponent.GetSkeletalMesh().Load<USkeletalMesh>();
        if (mesh is null) return null;

        var exportMesh = Mesh(mesh);
        if (exportMesh is null) return null;
        
        var overrideMaterials = meshComponent.GetOrDefault("OverrideMaterials", Array.Empty<UMaterialInterface?>());
        for (var idx = 0; idx < overrideMaterials.Length; idx++)
        {
            var material = overrideMaterials[idx];
            if (material is null) continue;

            exportMesh.OverrideMaterials.AddIfNotNull(Material(material, idx));
        }

        return exportMesh;
    }
    
    public ExportMesh? MeshComponent(UStaticMeshComponent meshComponent)
    {
        var mesh = meshComponent.GetStaticMesh().Load<UStaticMesh>();
        if (mesh is null) return null;

        var exportMesh = Mesh(mesh);
        if (exportMesh is null) return null;
        
        var overrideMaterials = meshComponent.GetOrDefault("OverrideMaterials", Array.Empty<UMaterialInterface?>());
        for (var idx = 0; idx < overrideMaterials.Length; idx++)
        {
            var material = overrideMaterials[idx];
            if (material is null) continue;
            if (OverridesToIgnore.Contains(material.Name)) continue;
            
            exportMesh.OverrideMaterials.AddIfNotNull(Material(material, idx));
        }

        return exportMesh;
    }

    public ExportMesh? Mesh(USkeletalMesh? mesh)
    {
        return Mesh<ExportMesh>(mesh);
    }

    public T? Mesh<T>(USkeletalMesh? mesh) where T : ExportMesh, new()
    {
        if (mesh is null) return null;
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportPart = new T
        {
            Name = mesh.Name,
            Path = Export(mesh),
            NumLods = convertedMesh.LODs.Count
        };

        var sections = convertedMesh.LODs[0].Sections.Value;
        foreach (var (index, section) in sections.Enumerate())
        {
            if (section.Material is null) continue;
            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            exportPart.Materials.AddIfNotNull(Material(material, index));
        }

        return exportPart;
    }

    public ExportMesh? Mesh(UStaticMesh? mesh)
    {
        return Mesh<ExportMesh>(mesh);
    }

    public T? Mesh<T>(UStaticMesh? mesh) where T : ExportMesh, new()
    {
        if (mesh is null) return null;
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportPart = new T
        {
            Name = mesh.Name,
            Path = Export(mesh),
            NumLods = convertedMesh.LODs.Count
        };

        var sections = convertedMesh.LODs[0].Sections.Value;
        foreach (var (index, section) in sections.Enumerate())
        {
            if (section.Material is null) continue;
            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            exportPart.Materials.AddIfNotNull(Material(material, index));
        }

        return exportPart;
    }

    public ExportMaterial? Material(UMaterialInterface material, int index)
    {
        return Material<ExportMaterial>(material, index);
    }

    public T? Material<T>(UMaterialInterface material, int index) where T : ExportMaterial, new()
    {
        if (!AppExportOptions.ExportMaterials) return null;

        var hash = material.GetPathName().GetHashCode();
        if (MaterialCache.FirstOrDefault(mat => mat.Hash == hash) is { } existing) return existing.Copy<T>() with { Slot = index };

        var absoluteParent = GetAbsoluteParent(material);
        var exportMaterial = new T
        {
            Path = material.GetPathName(),
            Name = material.Name,
            Slot = index,
            Hash = hash,
            AbsoluteParent = absoluteParent?.Name,
            UseGlassMaterial = absoluteParent is { BlendMode: EBlendMode.BLEND_Translucent, TranslucencyLightingMode: ETranslucencyLightingMode.TLM_SurfacePerPixelLighting or ETranslucencyLightingMode.TLM_VolumetricPerVertexDirectional }
        };

        AccumulateParameters(material, ref exportMaterial);
        MaterialCache.Add(exportMaterial);
        return exportMaterial;
    }

    public ExportOverrideMaterial? OverrideMaterial(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var exportMaterial = Material<ExportOverrideMaterial>(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        if (exportMaterial is null) return null;

        exportMaterial.MaterialNameToSwap = overrideData.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.Text.SubstringAfterLast(".");
        return exportMaterial;
    }
    
    public ExportOverrideParameters? OverrideParameters(FStructFallback overrideData)
    {
        var materialToAlter = overrideData.Get<FSoftObjectPath>("MaterialToAlter");
        if (materialToAlter.AssetPathName.IsNone) return null; 

        var exportParams = new ExportOverrideParameters();
        exportParams.MaterialNameToAlter = materialToAlter.AssetPathName.Text.SubstringAfterLast(".");
        AccumulateParameters(overrideData, ref exportParams);
        exportParams.Hash = exportParams.GetHashCode();
        return exportParams;
    }

    public UMaterial? GetAbsoluteParent(UMaterialInterface? materialInterface)
    {
        var parent = materialInterface;
        while (parent is UMaterialInstanceConstant materialInstance) parent = materialInstance.Parent as UMaterialInterface;
        return parent as UMaterial;
    }

    public void AccumulateParameters<T>(UMaterialInterface? materialInterface, ref T exportMaterial) where T : ExportParameterContainer
    {
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            foreach (var param in materialInstance.TextureParameterValues)
            {
                if (exportMaterial.Textures.Any(x => x.Name == param.Name)) continue;
                if (!param.ParameterValue.TryLoad(out UTexture texture)) continue;
                exportMaterial.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
            }

            foreach (var param in materialInstance.ScalarParameterValues)
            {
                if (exportMaterial.Scalars.Any(x => x.Name == param.Name)) continue;
                exportMaterial.Scalars.AddUnique(new ScalarParameter(param.Name, param.ParameterValue));
            }

            foreach (var param in materialInstance.VectorParameterValues)
            {
                if (exportMaterial.Vectors.Any(x => x.Name == param.Name)) continue;
                if (param.ParameterValue is null) continue;
                exportMaterial.Vectors.AddUnique(new VectorParameter(param.Name, param.ParameterValue.Value));
            }

            if (materialInstance.StaticParameters is not null)
            {
                foreach (var param in materialInstance.StaticParameters.StaticSwitchParameters)
                {
                    if (exportMaterial.Switches.Any(x => x.Name == param.Name)) continue;
                    exportMaterial.Switches.AddUnique(new SwitchParameter(param.Name, param.Value));
                }

                foreach (var param in materialInstance.StaticParameters.StaticComponentMaskParameters)
                {
                    if (exportMaterial.ComponentMasks.Any(x => x.Name == param.Name)) continue;

                    var color = new FLinearColor(
                        param.R ? 1.0f : 0.0f,
                        param.G ? 1.0f : 0.0f,
                        param.B ? 1.0f : 0.0f,
                        param.A ? 1.0f : 0.0f);
                    exportMaterial.ComponentMasks.AddUnique(new ComponentMaskParameter(param.Name, color));
                }
            }

            if (materialInstance.Parent is UMaterialInterface parentMaterial) AccumulateParameters(parentMaterial, ref exportMaterial);
        }
        else if (materialInterface is UMaterial material)
        {
            // TODO NORMAL MAT ACCUMULATION
        }
    }
    
    public void AccumulateParameters<T>(FStructFallback data, ref T exportMaterial) where T : ExportParameterContainer
    {
        var textureParams = data.GetOrDefault<FStyleParameter<FSoftObjectPath>[]>("TextureParams");
        foreach (var param in textureParams)
        {
            if (exportMaterial.Textures.Any(x => x.Name == param.Name)) continue;
            if (!param.Value.TryLoad(out UTexture texture)) continue;
            exportMaterial.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
        }

        var floatParams = data.GetOrDefault<FStyleParameter<float>[]>("FloatParams");
        foreach (var param in floatParams)
        {
            if (exportMaterial.Scalars.Any(x => x.Name == param.Name)) continue;
            exportMaterial.Scalars.AddUnique(new ScalarParameter(param.Name, param.Value));
        }

        var colorParams = data.GetOrDefault<FStyleParameter<FLinearColor>[]>("ColorParams");
        foreach (var param in colorParams)
        {
            if (exportMaterial.Vectors.Any(x => x.Name == param.ParamName.Text)) continue;
            exportMaterial.Vectors.AddUnique(new VectorParameter(param.Name, param.Value));
        }
    }

    public async Task<string> ExportAsync(UObject asset, bool waitForFinish = false)
    {
        var extension = asset switch
        {
            USkeletalMesh => AppExportOptions.MeshFormat switch
            {
                EMeshExportTypes.UEFormat => "uemodel",
                EMeshExportTypes.ActorX => "psk"
            },
            UStaticMesh => AppExportOptions.MeshFormat switch
            {
                EMeshExportTypes.UEFormat => "uemodel",
                EMeshExportTypes.ActorX => "pskx"
            },
            UTexture => AppExportOptions.ImageType switch
            {
                EImageType.PNG => "png",
                EImageType.TGA => "tga"
            }
        };

        var exportPath = GetExportPath(asset, extension);

        var returnValue = waitForFinish ? exportPath : asset.GetPathName();
        if (File.Exists(exportPath) && !(asset is UTexture texture && IsExportTextureHigherRes(texture, exportPath))) return returnValue;

        var exportTask = Task.Run(() =>
        {
            try
            {
                Export(asset, exportPath);
                Log.Information("Exporting {ExportType}: {Path}", asset.ExportType, exportPath);
            }
            catch (IOException e)
            {
                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.Message + e.StackTrace);
            }
        });
        
        ExportTasks.Add(exportTask);

        if (waitForFinish)
            exportTask.Wait();
        
        return returnValue;
    }

    private bool IsExportTextureHigherRes(UTexture texture, string path)
    {
        try
        {
            using var file = File.OpenRead(path);
            using var image = Image.FromStream(file, useEmbeddedColorManagement: false, validateImageData: false);
            var mip = texture.GetFirstMip();
            return mip?.SizeX > image.PhysicalDimension.Width && mip?.SizeY > image.PhysicalDimension.Height;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public string Export(UObject asset, bool waitForFinish = false)
    {
        return ExportAsync(asset, waitForFinish).GetAwaiter().GetResult();
    }

    private void Export(UObject asset, string exportPath)
    {
        switch (asset)
        {
            case USkeletalMesh skeletalMesh:
            {
                var exporter = new MeshExporter(skeletalMesh, FileExportOptions);
                exporter.TryWriteToDir(App.AssetsFolder, out _, out _);
                break;
            }
            case UStaticMesh staticMesh:
            {
                var exporter = new MeshExporter(staticMesh, FileExportOptions);
                exporter.TryWriteToDir(App.AssetsFolder, out _, out _);
                break;
            }
            case UTexture texture:
            {
                switch (AppExportOptions.ImageType)
                {
                    case EImageType.PNG:
                    {
                        using var fileStream = File.OpenWrite(exportPath);
                        var textureBitmap = texture.Decode();
                        textureBitmap?.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream); 
                        break;
                    }
                    case EImageType.TGA:
                        throw new NotImplementedException("TARGA (.tga) export not currently supported.");
                }

                break;
            }
        }
    }

    private static string GetExportPath(UObject obj, string ext, string extra = "")
    {
        var path = obj.Owner != null ? obj.Owner.Name : string.Empty;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(AppSettings.Current.GetExportPath(), path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        var finalPath = directory + $"{extra}.{ext.ToLower()}";
        return finalPath;
    }
}