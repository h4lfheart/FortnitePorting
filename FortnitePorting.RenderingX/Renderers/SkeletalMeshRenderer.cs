using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Data.Programs;
using FortnitePorting.RenderingX.Exceptions;
using FortnitePorting.RenderingX.Materials;

namespace FortnitePorting.RenderingX.Renderers;

public class SkeletalMeshRenderer : MeshRenderer
{
    public List<Section> Sections = [];
    public Material[] Materials = [];
    
    public SkeletalMeshRenderer(USkeletalMesh staticMesh, int lodLevel = 0) : base(new ShaderProgram("shader"))
    {
        if (!staticMesh.TryConvert(out var convertedMesh))
        {
            throw new RenderingXException("Failed to convert static mesh.");
        }

        BoundingBox = convertedMesh.BoundingBox;
        
        var lod = convertedMesh.LODs[Math.Min(lodLevel, convertedMesh.LODs.Count - 1)];
        
        Indices = lod.Indices!.Value;
        
        var vertices = lod.Verts;
        var extraUVs = lod.ExtraUV.Value;
        var buildVertices = new List<float>();
        for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            var vertex = vertices[vertexIndex];
            var position = vertex.Position * 0.01f;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;
            var uv = vertex.UV;
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
        
        
        var sections = lod.Sections.Value;
        Materials = new Material[sections.Length];

        for (var sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            var section = sections[sectionIndex];
            Sections.Add(new Section(section.MaterialIndex, section.NumFaces * 3, section.FirstIndex));

            if (staticMesh.Materials[section.MaterialIndex]?.TryLoad(out var sectionMaterial) ?? false)
            {
                Materials[sectionIndex] = sectionMaterial switch
                {
                    UMaterialInstanceConstant materialInstance => new Material(materialInstance),
                    UMaterial material => new Material(material),
                    _ => new Material()
                };
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
        
        foreach (var section in Sections)
        {
            Materials[section.MaterialIndex].Bind();
        }
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        base.RenderGeometry(camera);
        
        foreach (var section in Sections)
        {
            GL.DrawElements(PrimitiveType.Triangles, section.FaceCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr);
        }
    }
}