using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Data.Programs;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class GridComponent : MeshComponent
{
    public GridComponent()
    {
        Renderer = new GridMeshRenderer();
    }

}
file class GridMeshRenderer : MeshRenderer
{
    public GridMeshRenderer()
    {
        Shader = new ShaderProgram("grid");
        Vertices =
        [
            1f, 1f, 0f,
            -1f, -1f, 0f,
            -1f, 1f, 0f,
            -1f, -1f, 0f,
            1f, 1f, 0f,
            1f, -1f, 0f
        ];
        Indices = [0, 1, 2, 3, 4, 5];
        VertexAttributes =
        [
            new VertexAttribute("Position", 3, VertexAttribPointerType.Float)
        ];
    }

    protected override void RenderShader(CameraComponent camera)
    {
        Shader.Use();
        Shader.SetMatrix4("view", camera.GetViewMatrix(), transpose: false);
        Shader.SetMatrix4("proj", camera.GetProjectionMatrix(), transpose: false);
        Shader.SetUniform("uNear", camera.NearPlane);
        Shader.SetUniform("uFar", camera.FarPlane);
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        VertexArray.Bind();
        GL.DrawArrays(PrimitiveType.Triangles, 0, Indices.Length);
    }
}