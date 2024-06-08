using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.CUE4Parse;
using FortnitePorting.Shared.Models.Fortnite;
using FortnitePorting.Shared.Services;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Export;

public class ExportContext
{
    private List<Task> ExportTasks = [];
    private HashSet<ExportMaterial> MaterialCache = [];
    
    private readonly ExportMetaData AppExportOptions;
    private readonly ExporterOptions FileExportOptions;

    public ExportContext(ExportMetaData metaData)
    {
        AppExportOptions = metaData;
        FileExportOptions = AppExportOptions.Settings.CreateExportOptions();
    }
    
    public ExportPart? CharacterPart(UObject part)
    {
        var skeletalMesh = part.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
        if (skeletalMesh is null) return null;

        var exportPart = Mesh<ExportPart>(skeletalMesh);
        if (exportPart is null) return null;
        
        exportPart.Type = part.GetOrDefault("CharacterPartType", EFortCustomPartType.Head);
        exportPart.GenderPermitted = part.GetOrDefault("GenderPermitted", EFortCustomGender.Male);

        if (part.TryGetValue(out FStructFallback[] materialOverrides, "MaterialOverrides"))
        {
            foreach (var material in materialOverrides)
            {
                exportPart.OverrideMaterials.AddIfNotNull(OverrideMaterialIndexed(material));
            }
        }

        if (part.TryGetValue(out UObject additionalData, "AdditionalData"))
        {
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
                            PoseAsset(poseAssetNode.Get<UPoseAsset>("PoseAsset"), ref meta);
                        }
                        else if (skeletalMesh.ReferenceSkeleton.FinalRefBoneInfo.Any(bone => bone.Name.Text.Equals("FACIAL_C_FacialRoot", StringComparison.OrdinalIgnoreCase))
                                 && CUE4ParseVM.Provider.TryLoadObject("/BRCosmetics/Characters/Player/Male/Medium/Heads/M_MED_Jonesy3L_Head/Meshes/3L/3L_lod2_Facial_Poses_PoseAsset", out UPoseAsset poseAsset))
                        {
                            PoseAsset(poseAsset, ref meta);
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
        }
        
        return exportPart;
    }
    
    public void PoseAsset(UPoseAsset poseAsset, ref ExportHeadMeta meta)
    {
        /* Only tested when bAdditivePose = true */
        if (!poseAsset.bAdditivePose)
        {
            Log.Warning($"{poseAsset.Name}: bAdditivePose = false is unsupported");
            return;
        }

        var poseContainer = poseAsset.PoseContainer;
        var poses = poseContainer.Poses;
        if (poses.Length == 0)
        {
            Log.Warning($"{poseAsset.Name}: has no poses");
            return;
        }

        var poseNames = poseContainer.PoseFNames;
        if (poseNames is null || poseNames.Length == 0)
        {
            Log.Warning($"{poseAsset.Name}: PoseFNames is null or empty");
            return;
        }

        /* Assert number of tracks == number of bones for given skeleton */
        var poseTracks = poseContainer.Tracks;
        var poseTrackInfluences = poseContainer.TrackPoseInfluenceIndices;
        if (poseTracks.Length != poseTrackInfluences.Length)
        {
            Log.Warning($"{poseAsset.Name}: length of Tracks != length of TrackPoseInfluenceIndices");
            return;
        }

        /* Add poses by name first in order they appear */
        for (var i = 0; i < poses.Length; i++)
        {
            var pose = poses[i];
            var poseName = poseNames[i];
            var poseData = new PoseData(poseName.PlainText, pose.CurveData);
            meta.PoseData.Add(poseData);
        }

        /* Discover connection between bone name and relative location. */
        for (var i = 0; i < poseTrackInfluences.Length; i++)
        {
            var poseTrackInfluence = poseTrackInfluences[i];
            if (poseTrackInfluence is null) continue;

            var poseTrackName = poseTracks[i];
            foreach (var influence in poseTrackInfluence.Influences)
            {
                var pose = meta.PoseData[influence.PoseIndex];
                var transform = poses[influence.PoseIndex].LocalSpacePose[influence.BoneTransformIndex];
                if (!transform.Rotation.IsNormalized)
                    transform.Rotation.Normalize();

                pose.Keys.Add(new PoseKey(
                    poseTrackName.PlainText, /* Bone name to move */
                    transform.Translation,
                    transform.Rotation,
                    transform.Scale3D,
                    influence.PoseIndex,
                    influence.BoneTransformIndex
                ));
            }
        }

        if (poseAsset.RetargetSourceAssetReferencePose is null) return;
        if (poseAsset.Skeleton is null) return;
        if (!poseAsset.Skeleton.TryLoad<USkeleton>(out var skeleton)) return;

        var referenceMap = new Dictionary<string, int>(skeleton.ReferenceSkeleton.FinalNameToIndexMap, StringComparer.OrdinalIgnoreCase);
        foreach (var boneName in poseContainer.Tracks)
        {
            var boneNameStr = boneName.PlainText;
            if (!referenceMap.TryGetValue(boneNameStr, out var idx))
            {
                Log.Warning($"{poseAsset.Name}: {boneNameStr} missing from referenceSkeleton ({skeleton.Name})");
                continue;
            }

            if (idx >= poseAsset.RetargetSourceAssetReferencePose.Length)
            {
                Log.Warning($"{poseAsset.Name}: {boneNameStr} index {idx} is outside the bounds of the RetargetSourceAssetReferencePose");
                continue;
            }

            var transform = poseAsset.RetargetSourceAssetReferencePose[idx];
            meta.ReferencePose.Add(new ReferencePose(boneNameStr, transform.Translation, transform.Rotation, transform.Scale3D));
        }
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

    public ExportMaterial? Material(UMaterialInterface material, int index)
    {
        if (!AppExportOptions.Settings.ExportMaterials) return null;

        var hash = material.GetPathName().GetHashCode();
        if (MaterialCache.FirstOrDefault(mat => mat.Hash == hash) is { } existing) return existing with { Slot = index};

        var exportMaterial = new ExportMaterial
        {
            Path = material.GetPathName(),
            Name = material.Name,
            Slot = index,
            Hash = hash
        };

        AccumulateParameters(material, ref exportMaterial);

        exportMaterial.OverrideBlendMode = (material as UMaterialInstanceConstant)?.BasePropertyOverrides?.BlendMode ?? exportMaterial.BaseBlendMode;

        MaterialCache.Add(exportMaterial);
        return exportMaterial;
    }
    
    public ExportMaterial? OverrideMaterialIndexed(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var material = Material(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        return material;
    }
    
    public ExportOverrideMaterial? OverrideMaterialStyled(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var exportMaterial = Material(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        if (exportMaterial is null) return null;

        return new ExportOverrideMaterial
        {
            Material = exportMaterial,
            MaterialNameToSwap = overrideData.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.Text.SubstringAfterLast(".")
        };
    }
    
    public ExportOverrideParameters? OverrideParameters(FStructFallback overrideData)
    {
        var materialToAlter = overrideData.Get<FSoftObjectPath>("MaterialToAlter");
        if (materialToAlter.AssetPathName.IsNone) return null; 

        var exportParams = new ExportOverrideParameters();
        AccumulateParameters(overrideData, ref exportParams);
        
        exportParams.MaterialNameToAlter = materialToAlter.AssetPathName.Text.SubstringAfterLast(".");
        exportParams.Hash = exportParams.GetHashCode();
        return exportParams;
    }
    
    public void AccumulateParameters<T>(UMaterialInterface? materialInterface, ref T parameterCollection) where T : ParameterCollection
    {
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            foreach (var param in materialInstance.TextureParameterValues)
            {
                if (parameterCollection.Textures.Any(x => x.Name.Equals(param.Name))) continue;
                if (!param.ParameterValue.TryLoad(out UTexture texture)) continue;
                parameterCollection.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
            }

            foreach (var param in materialInstance.ScalarParameterValues)
            {
                if (parameterCollection.Scalars.Any(x => x.Name.Equals(param.Name))) continue;
                parameterCollection.Scalars.AddUnique(new ScalarParameter(param.Name, param.ParameterValue));
            }

            foreach (var param in materialInstance.VectorParameterValues)
            {
                if (parameterCollection.Vectors.Any(x => x.Name.Equals(param.Name))) continue;
                if (param.ParameterValue is null) continue;
                parameterCollection.Vectors.AddUnique(new VectorParameter(param.Name, param.ParameterValue.Value));
            }

            if (materialInstance.StaticParameters is not null)
            {
                foreach (var param in materialInstance.StaticParameters.StaticSwitchParameters)
                {
                    if (parameterCollection.Switches.Any(x => x.Name.Equals(param.Name))) continue;
                    parameterCollection.Switches.AddUnique(new SwitchParameter(param.Name, param.Value));
                }

                foreach (var param in materialInstance.StaticParameters.StaticComponentMaskParameters)
                {
                    if (parameterCollection.ComponentMasks.Any(x => x.Name.Equals(param.Name))) continue;

                    parameterCollection.ComponentMasks.AddUnique(new ComponentMaskParameter(param.Name, param.ToLinearColor()));
                }
            }
            
            // TODO optional segment loading
            /*if (materialInstance.TryLoadEditorData<UMaterialInstanceEditorOnlyData>(out var materialInstanceEditorData) && materialInstanceEditorData?.StaticParameters is not null)
            {
                foreach (var parameter in materialInstanceEditorData.StaticParameters.StaticSwitchParameters)
                {
                    if (parameter.ParameterInfo is null) continue;
                    parameterCollection.Switches.AddUnique(new SwitchParameter(parameter.Name, parameter.Value));
                }

                foreach (var parameter in materialInstanceEditorData.StaticParameters.StaticComponentMaskParameters)
                {
                    if (parameter.ParameterInfo is null) continue;
                    parameterCollection.ComponentMasks.AddUnique(new ComponentMaskParameter(parameter.Name, parameter.ToLinearColor()));
                }
            }*/

            if (materialInstance.Parent is UMaterialInterface parentMaterial) AccumulateParameters(parentMaterial, ref parameterCollection);
        }
        else if (materialInterface is UMaterial material)
        {
            if (parameterCollection is ExportMaterial exportMaterial)
            {
                exportMaterial.BaseMaterial = material;
            }
            
            // todo UMaterial parsing
        }
    }
    
    public void AccumulateParameters<T>(FStructFallback data, ref T parameterCollection) where T : ParameterCollection
    {
        var textureParams = data.GetOrDefault<FStyleParameter<FSoftObjectPath>[]>("TextureParams");
        foreach (var param in textureParams)
        {
            if (parameterCollection.Textures.Any(x => x.Name == param.Name)) continue;
            if (!param.Value.TryLoad(out UTexture texture)) continue;
            parameterCollection.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
        }

        var floatParams = data.GetOrDefault<FStyleParameter<float>[]>("FloatParams");
        foreach (var param in floatParams)
        {
            if (parameterCollection.Scalars.Any(x => x.Name == param.Name)) continue;
            parameterCollection.Scalars.AddUnique(new ScalarParameter(param.Name, param.Value));
        }

        var colorParams = data.GetOrDefault<FStyleParameter<FLinearColor>[]>("ColorParams");
        foreach (var param in colorParams)
        {
            if (parameterCollection.Vectors.Any(x => x.Name == param.ParamName.Text)) continue;
            parameterCollection.Vectors.AddUnique(new VectorParameter(param.Name, param.Value));
        }
    }

    public async Task<string> ExportAsync(UObject asset, bool returnRealPath = false, bool synchronousExport = false)
    {
        var extension = asset switch
        {
            USkeletalMesh or UStaticMesh or USkeleton => AppExportOptions.Settings.MeshFormat switch
            {
                EMeshFormat.UEFormat => "uemodel",
                EMeshFormat.ActorX => "psk",
                EMeshFormat.Gltf2 => "glb",
                EMeshFormat.OBJ => "obj",
            },
            UAnimSequence => AppExportOptions.Settings.AnimFormat switch
            {
                EAnimFormat.UEFormat => "ueanim",
                EAnimFormat.ActorX => "psa"
            },
            UTexture => AppExportOptions.Settings.ImageFormat switch
            {
                EImageFormat.PNG => "png",
                EImageFormat.TGA => "tga"
            },
            USoundWave => "wav"
        };

        var path = GetExportPath(asset, extension);
        Log.Information("Exporting {ExportType}: {Path}", asset.ExportType, path);
        
        var returnValue = returnRealPath ? path : asset.GetPathName();
        if (File.Exists(path)) return returnValue;

        var exportTask = TaskService.Run(() =>
        {
            try
            {
                Export(asset, path);
            }
            catch (IOException e)
            {
                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.Message + e.StackTrace);
            }
        });
        
        ExportTasks.Add(exportTask);

        if (synchronousExport)
            exportTask.Wait();

        return returnValue;
    }
    
    public string Export(UObject asset, bool returnRealPath = false, bool synchronousExport = false)
    {
        return ExportAsync(asset, returnRealPath, synchronousExport).GetAwaiter().GetResult();
    }

    private void Export(UObject asset, string path)
    {
        var assetsFolder = new DirectoryInfo(AppExportOptions.AssetsRoot);
        switch (asset)
        {
            case USkeletalMesh skeletalMesh:
            {
                var exporter = new MeshExporter(skeletalMesh, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case UStaticMesh staticMesh:
            {
                var exporter = new MeshExporter(staticMesh, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case USkeleton skeleton:
            {
                var exporter = new MeshExporter(skeleton, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case UAnimSequence animSequence:
            {
                var exporter = new AnimExporter(animSequence, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case UTexture texture:
            {
                using var fileStream = File.OpenWrite(path);
                var textureBitmap = texture.Decode();
                switch (AppExportOptions.Settings.ImageFormat)
                {
                    case EImageFormat.PNG:
                    {
                        textureBitmap?.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream); 
                        break;
                    }
                    case EImageFormat.TGA:
                    {
                        throw new NotImplementedException("TARGA (.tga) export not currently supported.");
                    }
                }

                break;
            }
            case USoundWave soundWave:
            {
                if (!SoundExtensions.TrySaveSoundToPath(soundWave, path))
                {
                    throw new Exception($"Failed to export sound '{soundWave.Name}' at {path}");
                }
                
                break;
            }
        }
    }
    
    public string GetExportPath(UObject obj, string ext)
    {
        var path = obj.Owner != null ? obj.Owner.Name : string.Empty;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(AppExportOptions.AssetsRoot, path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        var finalPath = $"{directory}.{ext.ToLower()}";
        return finalPath;
    }
}