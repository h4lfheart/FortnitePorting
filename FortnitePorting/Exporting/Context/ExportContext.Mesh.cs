using System;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
     public ExportMesh? Mesh(UObject obj)
    {
        return obj switch
        {
            USkeletalMesh skeletalMesh => Mesh(skeletalMesh),
            UStaticMesh staticMesh => Mesh(staticMesh),
            USkeleton skeleton => Skeleton(skeleton),
            _ => null
        };
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
        if (!mesh.TryConvert(out var convertedMesh, out var naniteLod, FileExportOptions.NaniteMeshFormat)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;
        
        var exportPart = new T
        {
            Name = mesh.Name,
            Path = Export(mesh, embeddedAsset: mesh.Owner?.Name.SubstringAfterLast("/") != mesh.Name, isNanite: naniteLod is not null),
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
    
    public ExportMesh? Skeleton(USkeleton? skeleton)
    {
        if (skeleton is null) return null;

        var exportMesh = new ExportMesh
        {
            Name = skeleton.Name,
            Path = Export(skeleton)
        };

        return exportMesh;
    }
    
    
    public ExportMesh? MeshComponent(UObject genericComponent)
    {
        return genericComponent switch
        {
            UInstancedStaticMeshComponent instancedStaticMeshComponent => MeshComponent(instancedStaticMeshComponent),
            USplineMeshComponent splineMeshComponent => MeshComponent(splineMeshComponent),
            UStaticMeshComponent staticMeshComponent => MeshComponent(staticMeshComponent),
            USkeletalMeshComponent skeletalMeshComponent => MeshComponent(skeletalMeshComponent),
            _ => null
        };
    }
    
    public ExportMesh? MeshComponent(USkeletalMeshComponent meshComponent)
    {
        var mesh = meshComponent.GetSkeletalMesh().Load<USkeletalMesh>();
        if (mesh is null) return null;

        var exportMesh = Mesh(mesh);
        if (exportMesh is null) return null;
        
        SetMeshComponentTransforms(exportMesh, meshComponent);

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

        var exportMesh = meshComponent is USplineMeshComponent splineComp ? MeshComponent(splineComp) : Mesh(mesh);
        if (exportMesh is null) return null;
        
        SetMeshComponentTransforms(exportMesh, meshComponent);

        var overrideMaterials = meshComponent.GetOrDefault("OverrideMaterials", Array.Empty<UMaterialInterface?>());
        for (var idx = 0; idx < overrideMaterials.Length; idx++)
        {
            var material = overrideMaterials[idx];
            if (material is null) continue;
            
            exportMesh.OverrideMaterials.AddIfNotNull(Material(material, idx));
        }

        if (meshComponent.LODData?.FirstOrDefault()?.OverrideVertexColors is { } overrideVertexColors)
        {
            exportMesh.OverrideVertexColors = overrideVertexColors.Data;
        }

        return exportMesh;
    }

    public ExportMesh? MeshComponent(UInstancedStaticMeshComponent instanceComponent)
    {
        var mesh = instanceComponent.GetOrDefault<UStaticMesh?>("StaticMesh");
        var exportMesh = Mesh(mesh);
        if (exportMesh is null) return null;
                
        SetMeshComponentTransforms(exportMesh, instanceComponent);

        foreach (var instance in instanceComponent.PerInstanceSMData ?? [])
        {
            exportMesh.Instances.Add(new ExportTransform(instance.TransformData));
        }

        return exportMesh;
    }
    
    public ExportMesh? MeshComponent(USplineMeshComponent? mesh)
    {
        return MeshComponent<ExportMesh>(mesh);
    }
    
    public T? MeshComponent<T>(USplineMeshComponent? mesh) where T : ExportMesh, new()
    {
        if (mesh is null) return null;
        if (!mesh.TryConvert(out var convertedMesh, out var naniteLod, FileExportOptions.NaniteMeshFormat)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportPart = new T
        {
            Name = mesh.Name,
            Path = Export(mesh, embeddedAsset: true, isNanite: naniteLod is not null),
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
    
    public void SetMeshComponentTransforms(ExportMesh exportMesh, USceneComponent meshComponent)
    {
        if (!exportMesh.IsEmpty)
        {
            var meshComponentAbsTransform = meshComponent.GetAbsoluteTransform();
            exportMesh.Location = meshComponentAbsTransform.Translation;
            exportMesh.Rotation = meshComponentAbsTransform.Rotator();
            exportMesh.Scale = meshComponentAbsTransform.Scale3D;
        }
        else
        {
            exportMesh.Location = meshComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector);
            exportMesh.Rotation = meshComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator);
            exportMesh.Scale = meshComponent.GetOrDefault("RelativeScale3D", FVector.OneVector);
        }
    }
    
    public ExportOverrideMorphTargets? OverrideMorphTargets(FStructFallback overrideData)
    {
        if (overrideData.TryGetValue<FName>(out var name, "Name") &&
            overrideData.TryGetValue<float>(out var value, "Value"))
            return new ExportOverrideMorphTargets(name.PlainText, value);

        return null;
    }
}