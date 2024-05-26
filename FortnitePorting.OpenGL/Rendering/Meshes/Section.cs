using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using FortnitePorting.OpenGL.Rendering.Levels;
using FortnitePorting.OpenGL.Rendering.Model;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Rendering.Meshes;

public class Section : VertexAndIndexModel
{
    private readonly Materials.Material? Material;

    public Section(CSkelMeshLod lod, CMeshSection section, UMaterialInterface? material, Matrix4? transform = null)
    {
        Transform = transform ?? Matrix4.Identity;
        
        var indices = lod.Indices.Value;
        for (var i = 0; i < section.NumFaces * 3; i++)
        {
            var index = indices[i + section.FirstIndex];
            Indices.Add((uint) index);
        }

        var extraUVs = lod.ExtraUV.Value;
        var vertexIndex = 0;
        foreach (var vertex in lod.Verts)
        {
            var position = vertex.Position * 0.01f;
            var texCoord = vertex.UV;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;
            var extraUV = extraUVs.Length > 0 ? extraUVs[0][vertexIndex] : new FMeshUVFloat(0, 0);

            Vertices.AddRange(new[]
            {
                position.X, position.Z, position.Y,
                texCoord.U, texCoord.V,
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y,
                extraUV.U, extraUV.V
            });

            vertexIndex++;
        }

        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);
        RegisterAttribute("UV1", 2, VertexAttribPointerType.Float);

        Shader = RenderManager.Instance.ObjectShader;
        Material = RenderManager.Instance.GetOrAddMaterial(material);
    }

    public Section(CStaticMeshLod lod, CMeshSection section, UMaterialInterface? material, TextureData? textureData = null, Matrix4? transform = null)
    {
        Transform = transform ?? Matrix4.Identity;
        var indices = lod.Indices.Value;
        for (var i = 0; i < section.NumFaces * 3; i++)
        {
            var index = indices[i + section.FirstIndex];
            Indices.Add((uint) index);
        }

        var extraUVs = lod.ExtraUV.Value;
        var vertexIndex = 0;
        foreach (var vertex in lod.Verts)
        {
            var position = vertex.Position * 0.01f;
            var texCoord = vertex.UV;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;
            var extraUV = extraUVs.Length > 0 ? extraUVs[0][vertexIndex] : new FMeshUVFloat(0, 0);

            Vertices.AddRange(new[]
            {
                position.X, position.Z, position.Y,
                texCoord.U, texCoord.V,
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y,
                extraUV.U, extraUV.V
            });

            vertexIndex++;
        }

        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);
        RegisterAttribute("UV1", 2, VertexAttribPointerType.Float);

        Shader = RenderManager.Instance.ObjectShader;
        Material =  RenderManager.Instance.GetOrAddMaterial(material, textureData);
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
        Shader.SetUniform3("viewVector", -camera.Direction);

        Material?.Bind(Shader);

        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
        GL.Enable(EnableCap.CullFace);
    }
}