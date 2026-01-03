namespace FortnitePorting.RenderingX.Data.Textures;


public class Texture(int width, int height, TextureTarget target, InternalFormat internalFormat, PixelFormat format, PixelType pixelType) : HandleContainer
{
    public int Width = width;
    public int Height = height;
    public TextureTarget Target = target;
    public InternalFormat InternalFormat = internalFormat;
    public PixelFormat Format = format;
    public PixelType PixelType = pixelType;
    
    public override void Generate()
    {
        _handle = GL.GenTexture();
    }

    public override void Delete()
    {
        GL.DeleteTexture(_handle);
    }

    public void Bind(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        Bind();
    }

    public void Bind()
    {
        GL.BindTexture(Target, _handle);
    }
}