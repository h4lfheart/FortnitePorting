using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Rendering.Cache;
using FortnitePorting.Rendering.Components.Rendering;
using FortnitePorting.Rendering.Data.Programs;
using FortnitePorting.Rendering.Exceptions;
using FortnitePorting.Rendering.Materials;

namespace FortnitePorting.Rendering.Renderers;

public class StaticMeshRenderer : MeshRenderer
{
    public List<Section> Sections = [];
    public Material[] Materials = [];

    public StaticMeshRenderer(UStaticMesh staticMesh, List<KeyValuePair<UBuildingTextureData, int>>? textureData = null, int lodLevel = 0) : base(new ShaderProgram("shader"))
    {
        using var convertedMesh = new StaticMeshDto(staticMesh, EMeshQuality.All, ENaniteMeshFormat.NoNanite);
        if (convertedMesh.LODs.Count == 0)
        {
            throw new RenderingXException("Failed to convert static mesh.");
        }

        BoundingBox = convertedMesh.Bounds;
        
        var lod = convertedMesh.LODs[Math.Min(lodLevel, convertedMesh.LODs.Count - 1)];
        
        Indices = lod.Indices;
        
        var vertices = lod.Vertices;
        var extraUVs = lod.ExtraUvs;
        var buildVertices = new List<float>();
        for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            var vertex = vertices[vertexIndex];
            var position = vertex.Position * 0.01f;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;
            var uv = vertex.Uv;
            var materialLayer = extraUVs.Length > 0 ? extraUVs[0][vertexIndex].U : 0;

            buildVertices.AddRange([
                position.X, position.Z, position.Y,
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y,
                uv.U, uv.V,
                materialLayer
            ]);
            
        }

        Vertices = buildVertices.ToArray();
        
        var sections = lod.Sections;
        Materials = new Material[sections.Length];
        if (Materials.Length == 0) return;

        for (var sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            var section = sections[sectionIndex];
            Sections.Add(new Section(section.MaterialIndex, section.NumFaces * 3, section.FirstIndex));

            if ((staticMesh.Materials[section.MaterialIndex]?.TryLoad(out var sectionMaterial) ?? false) && sectionMaterial is UMaterialInterface materialInterface)
            {
                var hasTextureData = sectionIndex == 0 && textureData?.Count > 0;
                Materials[sectionIndex] = hasTextureData ? MaterialCache.GetOrCreateWithTextureData(materialInterface, textureData!) :  MaterialCache.GetOrCreate(materialInterface);
            }
            else
            {
                Materials[sectionIndex] = new Material();
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
           
        foreach (var material in Materials)
        {
            Shader.Use();
            material.SetUniforms(Shader);
        }
    }

    protected override void BuildMesh()
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("MaterialLayer", 1, VertexAttribPointerType.Float);
        
        base.BuildMesh();
    }

    protected override void RenderShader(CameraComponent camera)
    {
        base.RenderShader(camera);
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        VertexArray.Bind();
        
        foreach (var section in Sections)
        {
            Materials[Math.Min(Materials.Length - 1, section.MaterialIndex)].Bind();
            GL.DrawElements(PrimitiveType.Triangles, section.FaceCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr);
        }
    }
}
