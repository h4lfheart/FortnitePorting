namespace FortnitePorting.RenderingX.Data.Buffers;

public class VertexArray<T> : HandleContainer where T : unmanaged
{
    public override void Generate()
    {
        _handle = GL.GenVertexArray();
    }

    public override void Delete()
    {
        GL.DeleteVertexArray(_handle);
    }
    
    public unsafe void VertexAttribPointer(uint index, int count, VertexAttribPointerType type, int stride, int offset)
    {
        GL.VertexAttribPointer(index, count, type, false, stride * sizeof(T), offset * sizeof(T));
        GL.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        GL.BindVertexArray(_handle);
    }
}