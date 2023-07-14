using System.Collections.Generic;
using System.Linq;
using FortnitePorting.OpenGL.Buffers;
using FortnitePorting.OpenGL.Shaders;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Renderable;

public record VertexAttribute(string Name, int Size, VertexAttribPointerType Type);

public abstract class Model : IRenderable
{
    private ProgramHandle Handle;
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

public abstract class VertexModel : Model
{
    public Buffer<float> VBO;
    public VertexArray<float> VAO;
    public List<float> Vertices = new();
    private readonly List<VertexAttribute> Attributes = new();

    public override void Setup()
    {
        base.Setup();
        VBO = new Buffer<float>(Vertices.ToArray(), BufferTargetARB.ArrayBuffer);

        VAO = new VertexArray<float>();

        var stride = Attributes.Sum(x => x.Size);
        var offset = 0;
        for (var i = 0; i < Attributes.Count; i++)
        {
            var attribute = Attributes[i];
            VAO.VertexAttribPointer((uint) i, attribute.Size, attribute.Type, stride, offset);
            offset += attribute.Size;
        }
    }

    protected void RegisterAttribute(string name, int count, VertexAttribPointerType type)
    {
        Attributes.Add(new VertexAttribute(name, count, type));
    }

    public override void Dispose()
    {
        base.Dispose();
        VBO.Dispose();
        VAO.Dispose();
    }
}

public abstract class VertexAndIndexModel : VertexModel
{
    public Buffer<uint> EBO;
    public List<uint> Indices = new();

    public override void Setup()
    {
        base.Setup();
        EBO = new Buffer<uint>(Indices.ToArray(), BufferTargetARB.ElementArrayBuffer);
    }

    public override void Dispose()
    {
        base.Dispose();
        EBO.Dispose();
    }
}