using FortnitePorting.Rendering.Rendering.Materials;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.Rendering.Rendering.Viewport;

public class Skybox : Meshes.BaseMesh
{
    public TextureCube Cubemap;
    
    public override List<float> Vertices => [
        -1, -1, -1,
         1, -1, -1,
         1,  1, -1,
        -1,  1, -1,
        -1, -1,  1,
         1, -1,  1,
         1,  1,  1,
        -1,  1,  1
    ];
    
    public override List<uint> Indices =>
    [
        0, 1, 3, 
        3, 1, 2,
        1, 5, 2, 
        2, 5, 6,
        5, 4, 6, 
        6, 4, 7,
        4, 0, 7, 
        7, 0, 3,
        3, 2, 7, 
        7, 2, 6,
        4, 5, 0, 
        0, 5, 1
    ];

    public Skybox() : base("skybox")
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        
        Cubemap = new TextureCube("px", "nx", "ny", "py", "pz", "nz");
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

        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
        GL.DepthFunc(DepthFunction.Less);
        GL.Enable(EnableCap.CullFace);
    }

    public override void Dispose()
    {
        base.Dispose();
        Cubemap.Dispose();
    }
}