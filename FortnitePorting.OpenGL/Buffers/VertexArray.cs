using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Buffers;

public class VertexArray<T> : IDisposable where T : unmanaged
{
    private readonly int Handle;

    public VertexArray()
    {
        Handle = GL.GenVertexArray();
        Bind();
    }

    public unsafe void VertexAttribPointer(uint index, int count, VertexAttribPointerType type, int stride, int offset)
    {
        GL.VertexAttribPointer(index, count, type, false, stride * sizeof(T), offset * sizeof(T));
        GL.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        GL.BindVertexArray(Handle);
    }

    public void UnBind()
    {
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(Handle);
    }
}