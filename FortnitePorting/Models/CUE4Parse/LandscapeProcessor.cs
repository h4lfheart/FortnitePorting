using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.Utils;
using FortnitePorting.Models.Unreal.Landscape;

namespace FortnitePorting.Models.CUE4Parse;

public class LandscapeProcessor
{
    public ALandscapeProxy LandscapeProxy;

    public ULandscapeComponent[] Components;
    
    public LandscapeProcessor(ALandscapeProxy landscapeProxy)
    {
        LandscapeProxy = landscapeProxy;
        Components = landscapeProxy.LandscapeComponents
            .Select(component => component.Load<ULandscapeComponent>())
            .Where(component => component is not null).ToArray()!;
        
    }

    public CStaticMesh Process()
    {
        var componentSize = LandscapeProxy.ComponentSizeQuads;
        
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        foreach (var component in Components)
        {
            if (componentSize == -1)
            {
                componentSize = component.ComponentSizeQuads;
            }
            
            component.GetExtent(ref minX, ref minY, ref maxX, ref maxY);
        }
        
        
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var uvScale = new FVector2D(1f, 1f) / new FVector2D(width, height);

        var vertexCountPerComponent = (int) Math.Pow(componentSize + 1, 2);
        var vertexCount = Components.Length * vertexCountPerComponent;
        var triangleCount = Components.Length * (int) Math.Pow(componentSize, 2) * 2;

        var material = LandscapeProxy.LandscapeMaterial?.Load();

        var lod = new CStaticMeshLod();
        lod.AllocateVerts(vertexCount);
        lod.NumTexCoords = 2;
        lod.Sections = new Lazy<CMeshSection[]>(() => [new CMeshSection(0, 0, triangleCount, material?.Name ?? string.Empty, material is not null ? new ResolvedLoadedObject(material) : null)]);
        
        var selectedComponentIndex = 0;
        var extraVertexColors = new Dictionary<string, CVertexColor>();
        foreach (var component in Components)
        {
            var accessor = new FLandscapeComponentDataInterface(component);
            var baseVertexIndex = selectedComponentIndex * vertexCountPerComponent;
            
            for (var vertexIndex = 0; vertexIndex < vertexCountPerComponent; vertexIndex++)
            {
                accessor.VertexIndexToXY(vertexIndex, out var vertX, out var vertY);
                
                var position = accessor.GetLocalVertex(vertX, vertY, accessor.HeightMipData) + component.RelativeLocation;
                
                accessor.GetLocalTangentVectors(vertX, vertY, out var tangent, out var binormal, out var normal, accessor.HeightMipData);

                var globalUV = new FVector2D(vertX + component.SectionBaseX, vertY + component.SectionBaseY);
                var sectionUV = (globalUV - new FVector2D(minX, minY)) * uvScale;

                var vertex = new CMeshVertex(position, new FVector4(normal), new FVector4(tangent), new FMeshUVFloat(globalUV.X, globalUV.Y));
                lod.Verts[baseVertexIndex + vertexIndex] = vertex;
                lod.ExtraUV.Value[0][baseVertexIndex + vertexIndex] = new FMeshUVFloat(sectionUV.X, sectionUV.Y);

                foreach (var weightMapAllocation in component.WeightmapLayerAllocations)
                {
                    var weight = accessor.GetLayerWeight(vertX, vertY, weightMapAllocation);

                    var layerName = weightMapAllocation.LayerInfo.Name;
                    if (!extraVertexColors.ContainsKey(layerName))
                    {
                        var properName = layerName.SubstringBefore("_LayerInfo");
                        extraVertexColors.TryAdd(layerName, new CVertexColor(properName, new FColor[vertexCount]));
                    }

                    extraVertexColors[layerName].ColorData[baseVertexIndex + vertexIndex] = new FColor(weight, weight, weight, weight);
                }
                
            }

            selectedComponentIndex++;
        }

        var indices = new List<uint>();
        for (var componentIndex = 0; componentIndex < Components.Length; componentIndex++)
        {
            var baseVertexIndex = componentIndex * vertexCountPerComponent;

            for (var y = 0; y < componentSize; y++)
            {
                for (var x = 0; x < componentSize; x++)
                {
                    indices.Add((uint) (baseVertexIndex + (x + 0) + (y + 0) * (componentSize + 1)));
                    indices.Add((uint) (baseVertexIndex + (x + 1) + (y + 1) * (componentSize + 1)));
                    indices.Add((uint) (baseVertexIndex + (x + 1) + (y + 0) * (componentSize + 1)));
                    
                    indices.Add((uint) (baseVertexIndex + (x + 0) + (y + 0) * (componentSize + 1)));
                    indices.Add((uint) (baseVertexIndex + (x + 0) + (y + 1) * (componentSize + 1)));
                    indices.Add((uint) (baseVertexIndex + (x + 1) + (y + 1) * (componentSize + 1)));
                }
            }
        }

        lod.Indices = new Lazy<uint[]>(indices.ToArray());

        lod.ExtraVertexColors = extraVertexColors.Values.ToArray();

        var mesh = new CStaticMesh();
        mesh.LODs.Add(lod);

        return mesh;
    }
}