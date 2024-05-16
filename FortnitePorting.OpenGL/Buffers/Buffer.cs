
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Buffers;

public class Buffer<T> : IDisposable where T : unmanaged
{
    private readonly int Handle;
    private readonly T[] Source;
    private readonly BufferTarget Target;

    public Buffer(T[] source, BufferTarget bufferTarget)
    {
        Source = source;
        Target = bufferTarget;
        Handle = GL.GenBuffer();
        Bind();
        GL.BufferData(Target, Source, BufferUsage.StaticDraw);
    }

    public int Size() => Source.Length;

    public void Bind()
    {
        GL.BindBuffer(Target, Handle);
    }

    public void UnBind()
    {
        GL.BindBuffer(Target, 0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(Handle);
    }
}