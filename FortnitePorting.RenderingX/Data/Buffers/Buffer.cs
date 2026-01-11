namespace FortnitePorting.RenderingX.Data.Buffers;

public unsafe class Buffer<T>(BufferTarget _bufferTarget) : HandleContainer
    where T : unmanaged
{
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
        
        GL.BufferData(_bufferTarget, data.Length * sizeof(T), data, usage);
    }

    public void Bind()
    {
        GL.BindBuffer(_bufferTarget, _handle);
    }

}