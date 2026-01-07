using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Data.Programs;

namespace FortnitePorting.RenderingX.Renderers;

public class GridRenderer : MeshRenderer
{
    public float GridScale1 = 0.25f;
    public float GridScale2  = 1.0f;
    public Vector3 GridColor1 =  new(67f / 255f, 67f / 255f, 67f / 255f);
    public Vector3 GridColor2 = new(0f / 255f, 0f / 255f, 0f / 255f);
    public float FadeStart = 0.0f;
    public float FadeEnd = 250.0f;
    
    private static readonly ShaderProgram _shader = new("grid");
    
    public GridRenderer() : base(_shader)
    {
        Vertices = [
            -1f, -1f, 0f,
             1f, -1f, 0f,
             1f,  1f, 0f,
            -1f,  1f, 0f
        ];
        
        Indices = [0, 1, 2, 2, 3, 0];
        
        VertexAttributes = [
            new VertexAttribute("Position", 3, VertexAttribPointerType.Float)
        ];
    }

    protected override void RenderShader(CameraComponent camera)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        
        Shader.Use();
        Shader.SetMatrix4("u_View", camera.ViewMatrix(), transpose: false);
        Shader.SetMatrix4("u_Proj", camera.ProjectionMatrix(), transpose: false);
        Shader.SetUniform("u_Near", camera.NearPlane);
        Shader.SetUniform("u_Far", camera.FarPlane);
        Shader.SetUniform3("u_CameraPos", camera.Actor.GetComponent<SpatialComponent>()!.WorldPosition());
        
        Shader.SetUniform("u_GridScale1", GridScale1);
        Shader.SetUniform("u_GridScale2", GridScale2);
        Shader.SetUniform3("u_GridColor1", GridColor1);
        Shader.SetUniform3("u_GridColor2", GridColor2);
        Shader.SetUniform("u_FadeStart", FadeStart);
        Shader.SetUniform("u_FadeEnd", FadeEnd);
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        GL.DepthMask(false);
        
        base.RenderGeometry(camera);
        
        GL.DepthMask(true);
    }
}