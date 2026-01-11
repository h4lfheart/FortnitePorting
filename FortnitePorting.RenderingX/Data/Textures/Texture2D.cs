namespace FortnitePorting.RenderingX.Data.Textures;

public class Texture2D(
    int width,
    int height,
    byte[] pixels,
    InternalFormat internalFormat = InternalFormat.Rgba,
    PixelFormat format = PixelFormat.Rgba,
    PixelType pixelType = PixelType.UnsignedByte
) : Texture(width, height, TextureTarget.Texture2d, internalFormat, format, pixelType)
{
    public override void Generate()
    {
        base.Generate();
        
        Bind();
        GL.TexImage2D(Target, 0, InternalFormat, Width, Height, 0, Format, PixelType, pixels);
        
        GL.GenerateMipmap(TextureTarget.Texture2d);
        
        GL.TexParameteri(Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameteri(Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        
        GL.TexParameteri(Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameteri(Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

    }
}