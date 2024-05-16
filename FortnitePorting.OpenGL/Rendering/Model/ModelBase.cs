using FortnitePorting.OpenGL.Materials;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Rendering.Model;

public class ModelBase : IRenderable
{
    private int Handle;
    public Shader Shader;
    public Matrix4 Transform;

    public virtual void Setup()
    {
        Handle = GL.CreateProgram();
    }

    public virtual void Render(Camera camera)
    {
    }

    public virtual void Dispose()
    {
        GL.DeleteProgram(Handle);
        Shader.Dispose();
    }
}