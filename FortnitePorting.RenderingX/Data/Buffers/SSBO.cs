namespace FortnitePorting.RenderingX.Data.Buffers;

public unsafe class SSBO<T>(uint _bindingPoint = 0) : HandleContainer
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

    public void Fill(T[] data, BufferUsage usage = BufferUsage.DynamicDraw)
    {
        Bind();
        GL.BufferData(BufferTarget.ShaderStorageBuffer, data.Length * sizeof(T), data, usage);
    }

    public void Bind()
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _handle);
    }

    public void BindBufferBase()
    {
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, _bindingPoint, _handle);
    }
}