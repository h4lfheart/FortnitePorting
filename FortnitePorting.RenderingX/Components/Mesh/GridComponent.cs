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
    public float GridScale1 = 0.25f;
    public float GridScale2  = 1.0f;
    public Vector3 GridColor1 =  new(67f / 255f, 67f / 255f, 67f / 255f);
    public Vector3 GridColor2 = new(0f / 255f, 0f / 255f, 0f / 255f);
    public float FadeStart = 250.0f;
    public float FadeEnd = 500.0f;
    
    public GridMeshRenderer()
    {
        Shader = new ShaderProgram("grid");
        
        // Full-screen quad vertices
        Vertices = [
            -1f, -1f, 0f,  // bottom-left
             1f, -1f, 0f,  // bottom-right
             1f,  1f, 0f,  // top-right
            -1f,  1f, 0f   // top-left
        ];
        
        Indices = [0, 1, 2, 2, 3, 0];
        
        VertexAttributes = [
            new VertexAttribute("Position", 3, VertexAttribPointerType.Float)
        ];
    }

    protected override void RenderShader(CameraComponent camera)
    {
        
        GL.DepthMask(false);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        
        Shader.Use();
        Shader.SetMatrix4("u_View", camera.GetViewMatrix(), transpose: false);
        Shader.SetMatrix4("u_Proj", camera.GetProjectionMatrix(), transpose: false);
        Shader.SetUniform("u_Near", camera.NearPlane);
        Shader.SetUniform("u_Far", camera.FarPlane);
        Shader.SetUniform3("u_CameraPos", camera.Owner.Transform!.WorldPosition);
        
        // Grid properties
        Shader.SetUniform("u_GridScale1", GridScale1);
        Shader.SetUniform("u_GridScale2", GridScale2);
        Shader.SetUniform3("u_GridColor1", GridColor1);
        Shader.SetUniform3("u_GridColor2", GridColor2);
        Shader.SetUniform("u_FadeStart", FadeStart);
        Shader.SetUniform("u_FadeEnd", FadeEnd);
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        VertexArray.Bind();
        GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
        
        // Restore default OpenGL state
        GL.DepthMask(true);
    }
}