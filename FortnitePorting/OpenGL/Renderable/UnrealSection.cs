using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.OpenGL.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Renderable;

public class UnrealSection : VertexAndIndexModel
{
    private Material? Material;
    public UnrealSection(CSkelMeshLod lod, CMeshSection section, UMaterialInterface? material, Matrix4? transform = null)
    {
        Transform = transform ?? Matrix4.Identity;
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
    
    public UnrealSection(CStaticMeshLod lod, CMeshSection section, UMaterialInterface? material, Matrix4? transform = null)
    {
        Transform = transform ?? Matrix4.Identity;
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
            var normal = (FVector) vertex.Normal;
            var tangent = (FVector) vertex.Tangent;

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
        GL.Disable(EnableCap.CullFace);
        
        VAO.Bind();
        Shader.Use();
        
        Shader.SetMatrix4("uTransform", Transform);
        Shader.SetMatrix4("uView", camera.GetViewMatrix());
        Shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        Shader.SetUniform("diffuseTex", 0);
        Shader.SetUniform("normalTex", 1);
        Shader.SetUniform("specularTex", 2);
        Shader.SetUniform("maskTex", 3);
        Shader.SetUniform("environmentTex", 4);
        Shader.SetUniform3("viewVector", -camera.Direction);
        Shader.SetUniform("isGlass", Material is { IsGlass: true } ? 1 : 0);
        
        Material?.Bind();

        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
        GL.Enable(EnableCap.CullFace);
    }
}  