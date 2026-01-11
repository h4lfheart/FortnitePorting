namespace FortnitePorting.RenderingX.Data.Programs;


public class Program : HandleContainer
{
    public override void Generate()
    {
        _handle = GL.CreateProgram();
    }

    public override void Delete()
    {
        GL.DeleteProgram(_handle);
    }

    public virtual void Link()
    {
        GL.LinkProgram(_handle);
    }

    public void Use()
    {
        GL.UseProgram(_handle);
    }
}