using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Rendering.Viewport;

public class Grid : Meshes.BaseMesh
{
    public override List<float> Vertices => [
         1f,  1f, 0f,
        -1f, -1f, 0f,
        -1f,  1f, 0f,
        -1f, -1f, 0f,
         1f,  1f, 0f,
         1f, -1f, 0f
    ];

    public override List<uint> Indices => [0, 1, 2, 3, 4, 5];

    public Grid() : base("grid")
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
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