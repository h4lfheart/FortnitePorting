using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.OpenGL.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Renderable;

public class UnrealSection : VertexAndIndexModel
{
    private Material? Material;
    public UnrealSection(CSkelMeshLod lod, CMeshSection section, UMaterialInterface? material)
    {
        var indices = lod.Indices.Value;
        for (var i = 0; i < section.NumFaces * 3; i++)
        {
            var index = indices[i + section.FirstIndex];
            Indices.Add((uint)index);
        }

        foreach (var vertex in lod.Verts)
        {
            var position = vertex.Position * 0.01f;
            var texCoord = vertex.UV;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;

            Vertices.AddRange(new[] { 
                position.X, position.Z, position.Y, 
                texCoord.U, texCoord.V, 
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y
            });
        }
        
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);

        Shader = AppVM.MeshViewer.Renderer.MasterShader;
        Material = AppVM.MeshViewer.Renderer.GetOrAddMaterial(material);
    }
    
    public UnrealSection(CStaticMeshLod lod, CMeshSection section, UMaterialInterface? material)
    {
        Log.Information(material.Name);
        var indices = lod.Indices.Value;
        for (var i = 0; i < section.NumFaces * 3; i++)
        {
            var index = indices[i + section.FirstIndex];
            Indices.Add((uint)index);
        }

        foreach (var vertex in lod.Verts)
        {
            var position = vertex.Position * 0.01f;
            var texCoord = vertex.UV;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;

            Vertices.AddRange(new[] { 
                position.X, position.Z, position.Y, 
                texCoord.U, texCoord.V, 
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y
            });
        }
        
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);

        Shader = AppVM.MeshViewer.Renderer.MasterShader;
        Material = AppVM.MeshViewer.Renderer.GetOrAddMaterial(material);
    }

    public override void Render(Camera camera)
    {
        base.Render(camera);
        
        VAO.Bind();
        Shader.Use();
        
        Shader.SetMatrix4("uTransform", Matrix4.Identity);
        Shader.SetMatrix4("uView", camera.GetViewMatrix());
        Shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        Shader.SetUniform("diffuseTex", 0);
        Shader.SetUniform("normalTex", 1);
        Shader.SetUniform("specularTex", 2);
        Shader.SetUniform("maskTex", 3);
        
        Material?.Bind();

        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
    }
}  