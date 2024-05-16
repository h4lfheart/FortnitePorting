using FortnitePorting.OpenGL.Materials;
using FortnitePorting.OpenGL.Rendering.Model;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Rendering.World;

public class Grid : VertexAndIndexModel
{
    public Grid()
    {
        Indices = [0, 1, 2, 3, 4, 5];

        Vertices =
        [
            1f, 1f, 0f,
            -1f, -1f, 0f,
            -1f, 1f, 0f,
            -1f, -1f, 0f,
            1f, 1f, 0f,
            1f, -1f, 0
        ];

        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);

        Shader = new Shader("grid");
        Shader.Use();
    }

    public override void Render(Camera camera)
    {
        GL.Disable(EnableCap.CullFace);
        VAO.Bind();

        Shader.Use();

        Shader.SetMatrix4("view", camera.GetViewMatrix(), transpose: false);
        Shader.SetMatrix4("proj", camera.GetProjectionMatrix(), transpose: false);
        Shader.SetUniform("uNear", camera.Near);
        Shader.SetUniform("uFar", camera.Far);

        GL.DrawArrays(PrimitiveType.Triangles, 0, Indices.Count);
        GL.Enable(EnableCap.CullFace);
    }
}