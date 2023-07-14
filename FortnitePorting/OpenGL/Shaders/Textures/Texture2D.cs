using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Shaders.Textures;

public class Texture2D
{
    public static readonly Texture2D Diffuse = new(new FLinearColor(0.8f, 0.8f, 0.8f, 1.0f));
    public static readonly Texture2D Normals = new(new FLinearColor(0.5f, 0.5f, 1.0f, 1.0f));
    public static readonly Texture2D SpecularMasks = new(new FLinearColor(0.5f, 0.0f, 0.5f, 1.0f));
    public static readonly Texture2D Mask = new(new FLinearColor(1.0f, 0.5f, 0.0f, 1.0f));
    private readonly TextureHandle Handle;

    public Texture2D(UTexture2D texture)
    {
        Handle = GL.GenTexture();
        Bind();

        var firstMip = texture.GetFirstMip();
        TextureDecoder.DecodeTexture(firstMip, texture.Format, texture.IsNormalMap, ETexturePlatform.DesktopMobile, out var data, out _);

        GL.TexImage2D(TextureTarget.Texture2d, 0, texture.SRGB ? InternalFormat.Srgb : InternalFormat.Rgb, firstMip.SizeX, firstMip.SizeY, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
    }

    public Texture2D(FLinearColor color)
    {
        Handle = GL.GenTexture();
        Bind();

        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, 1, 1, 0, PixelFormat.Rgb, PixelType.Float, new[] { color.R, color.G, color.B });

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
    }

    public void Bind(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        Bind();
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