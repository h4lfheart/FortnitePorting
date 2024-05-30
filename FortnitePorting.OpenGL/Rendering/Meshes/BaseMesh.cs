using FortnitePorting.OpenGL.Buffers;
using FortnitePorting.OpenGL.Rendering.Materials;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Rendering.Meshes;

public class BaseMesh : IRenderable
{
    private int Handle;
    public Shader Shader;
    
    public virtual List<float> Vertices { get; set; } = [];
    public virtual List<uint> Indices { get; set; } = [];
    
    public Buffer<float> VBO;
    public Buffer<uint> EBO;
    public VertexArray<float> VAO;
    
    public Matrix4 Transform = Matrix4.Identity;
    
    private readonly List<VertexAttribute> Attributes = [];

    public BaseMesh(string shaderName) : this(new Shader(shaderName)) { }
    
    public BaseMesh(Shader shader)
    {
        Shader = shader;
    }

    public virtual void Setup()
    {
        Handle = GL.CreateProgram();
            
        VBO = new Buffer<float>(Vertices.ToArray(), BufferTarget.ArrayBuffer);
        VAO = new VertexArray<float>();

        var stride = Attributes.Sum(x => x.Count);
        var offset = 0;
        for (var i = 0; i < Attributes.Count; i++)
        {
            var attribute = Attributes[i];
            VAO.VertexAttribPointer((uint) i, attribute.Count, attribute.Type, stride, offset);
            offset += attribute.Count;
        }
        
        EBO = new Buffer<uint>(Indices.ToArray(), BufferTarget.ElementArrayBuffer);
    }

    protected void RegisterAttribute(string name, int count, VertexAttribPointerType type)
    {
        Attributes.Add(new VertexAttribute(name, count, type));
    }

    public virtual void Render(Camera camera)
    {
    }

    public virtual void Dispose()
    {
        GL.DeleteProgram(Handle);
        VBO.Dispose();
        VAO.Dispose();
        EBO.Dispose();
        Shader.Dispose();
    }
}

public record VertexAttribute(string Name, int Count, VertexAttribPointerType Type);