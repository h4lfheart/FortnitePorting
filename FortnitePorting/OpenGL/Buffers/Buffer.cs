using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Buffers;

public class Buffer<T> : IDisposable where T : unmanaged
{
    private readonly BufferHandle Handle;
    private T[] Source;
    private BufferTargetARB Target;
    
    public Buffer(T[] source, BufferTargetARB bufferTarget)
    {
        Source = source;
        Target = bufferTarget;
        Handle = GL.GenBuffer();
        Bind();
        GL.BufferData(Target, Source, BufferUsageARB.StaticDraw);
    }

    public int Size() => Source.Length;

    public void Bind()
    {
        GL.BindBuffer(Target, Handle);
    }

    public void UnBind()
    {
        GL.BindBuffer(Target, BufferHandle.Zero);
    }
    
    public void Dispose()
    {
        GL.DeleteBuffer(Handle);
    }
}