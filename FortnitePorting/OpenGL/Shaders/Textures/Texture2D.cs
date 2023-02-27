using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Shaders.Textures;

public class Texture2D
{
    private readonly TextureHandle Handle;
    
    public Texture2D(UTexture2D texture)
    {
        Handle = GL.GenTexture();
        Bind();
        
        var firstMip = texture.GetFirstMip();
        TextureDecoder.DecodeTexture(firstMip, texture.Format, texture.isNormalMap, ETexturePlatform.DesktopMobile, out var data, out _);
        
        GL.TexImage2D(TextureTarget.Texture2d, 0, texture.SRGB ? InternalFormat.Srgb : InternalFormat.Rgb, firstMip.SizeX, firstMip.SizeY, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
    }

    public void Bind(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2d, Handle);
    }
    
    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2d, Handle);
    }

    public void Dispose()
    {
        GL.DeleteTexture(Handle);
    }
}