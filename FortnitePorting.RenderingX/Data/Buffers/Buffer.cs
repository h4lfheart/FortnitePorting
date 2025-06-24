namespace FortnitePorting.RenderingX.Data.Buffers;

public unsafe class Buffer<T> : HandleContainer where T : unmanaged
{
    private readonly BufferTarget Target;

    public Buffer(BufferTarget bufferTarget)
    {
        Target = bufferTarget;
    }

    public override void Generate()
    {
        _handle = GL.GenBuffer();
    }

    public override void Delete()
    {
        GL.DeleteBuffer(_handle);
    }

    public void Fill(T[] data, BufferUsage usage = BufferUsage.StaticDraw)
    {
        Bind();
        
        GL.BufferData(Target, data.Length * sizeof(T), data, usage);
    }

    public void Bind()
    {
        GL.BindBuffer(Target, _handle);
    }

}