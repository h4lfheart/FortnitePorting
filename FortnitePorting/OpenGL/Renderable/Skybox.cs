using System.Collections.Generic;
using FortnitePorting.OpenGL.Shaders;
using FortnitePorting.OpenGL.Shaders.Textures;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Renderable;

public class Skybox : VertexModel
{
    public CubemapTexture Cubemap;

    public Skybox()
    {
        Vertices = new List<float>
        {
            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, 0.5f, -0.5f,
            0.5f, 0.5f, -0.5f,
            -0.5f, 0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,

            -0.5f, -0.5f, 0.5f,
            0.5f, -0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, 0.5f,
            -0.5f, -0.5f, 0.5f,

            -0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, 0.5f,
            -0.5f, 0.5f, 0.5f,

            0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,

            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, 0.5f,
            0.5f, -0.5f, 0.5f,
            -0.5f, -0.5f, 0.5f,
            -0.5f, -0.5f, -0.5f,

            -0.5f, 0.5f, -0.5f,
            0.5f, 0.5f, -0.5f,
            0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, -0.5f
        };

        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);

        Shader = new Shader("skybox");
        Shader.Use();

        Cubemap = new CubemapTexture("px", "nx", "ny", "py", "pz", "nz");
    }

    public override void Render(Camera camera)
    {
        base.Render(camera);
        GL.Disable(EnableCap.CullFace);
        GL.DepthFunc(DepthFunction.Lequal);

        VAO.Bind();
        Shader.Use();
        Cubemap.Bind(TextureUnit.Texture0);

        Shader.SetMatrix4("uTransform", Matrix4.Identity);
        var viewMatrix = camera.GetViewMatrix() with
        {
            M41 = 0,
            M42 = 0,
            M43 = 0
        };

        Shader.SetMatrix4("uView", viewMatrix);
        Shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());

        GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Count);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.CullFace);
    }

    public override void Dispose()
    {
        base.Dispose();
        Cubemap.Dispose();
    }
}