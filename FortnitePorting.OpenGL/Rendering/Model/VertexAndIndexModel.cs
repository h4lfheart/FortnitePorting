using FortnitePorting.OpenGL.Buffers;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Rendering.Model;

public abstract class VertexAndIndexModel : VertexModel
{
    public Buffer<uint> EBO;
    public List<uint> Indices = new();

    public override void Setup()
    {
        base.Setup();
        EBO = new Buffer<uint>(Indices.ToArray(), BufferTarget.ElementArrayBuffer);
    }

    public override void Dispose()
    {
        base.Dispose();
        EBO.Dispose();
    }
}