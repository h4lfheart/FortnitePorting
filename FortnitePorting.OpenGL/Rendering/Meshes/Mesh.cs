using System.Diagnostics;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Material;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Rendering.Meshes;

public class Mesh : BaseMesh
{
    public List<Section> Sections = [];
    public Materials.Material[] Materials = [];
    
    private const float SCALE = 0.01f;
    
    public Mesh(CBaseMeshLod lod, CMeshVertex[] vertices, ResolvedObject?[] materials) : base(RenderManager.Instance.ObjectShader)
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);
        RegisterAttribute("MaterialLayer", 1, VertexAttribPointerType.Float);
        
        var indices = lod.Indices.Value;
        for (var i = 0; i < indices.Length; i++)
        {
            Indices.Add((uint) indices[i]);
        }

        var extraUVs = lod.ExtraUV.Value;
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            
            var position = vertex.Position * SCALE;
            var texCoord = vertex.UV;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;
            var materialLayer = extraUVs.Length > 0 ? extraUVs[0][i].U : 0;

            Vertices.AddRange(new[]
            {
                position.X, position.Z, position.Y,
                texCoord.U, texCoord.V,
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y,
                materialLayer
            });
        }

        foreach (var section in lod.Sections.Value)
        {
            Sections.Add(new Section(section.MaterialIndex, section.NumFaces * 3, section.FirstIndex));
        }

        if (Materials.Length == 0) // only if materials havent been passed in from external means i.e. actor
        {
            Materials = new Materials.Material[materials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material is null) continue;

                if (material.TryLoad(out var materialObject) && materialObject is UMaterialInterface materialInterface)
                {
                    Materials[i] = RenderManager.Instance.GetOrAddMaterial(materialInterface);
                }
            }
        }
    }
    
    public override void Render(Camera camera)
    {
        base.Render(camera);
        GL.Disable(EnableCap.CullFace);
        
        Shader.Use();
        Shader.SetMatrix4("uTransform", Transform);
        Shader.SetMatrix4("uView", camera.GetViewMatrix());
        Shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        Shader.SetUniform3("viewVector", -camera.Direction);

        VAO.Bind();
        foreach (var section in Sections)
        {
            Materials[section.MaterialIndex].Render(Shader);
            GL.DrawElements(PrimitiveType.Triangles, section.FaceCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr);
        }

        GL.Enable(EnableCap.CullFace);
    }
}