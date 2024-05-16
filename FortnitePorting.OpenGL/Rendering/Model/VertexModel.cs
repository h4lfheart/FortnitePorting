using FortnitePorting.OpenGL.Buffers;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Rendering.Model;

public record VertexAttribute(string Name, int Size, VertexAttribPointerType Type);

public class VertexModel : ModelBase
{
    public Buffer<float> VBO;
    public VertexArray<float> VAO;
    public List<float> Vertices = [];
    private readonly List<VertexAttribute> Attributes = [];

    public override void Setup()
    {
        base.Setup();
        VBO = new Buffer<float>(Vertices.ToArray(), BufferTarget.ArrayBuffer);

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
