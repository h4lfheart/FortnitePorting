using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.Rendering.Rendering.Materials;

public class Texture2D
{
    public static Texture2D Diffuse => new(new FLinearColor(0.8f, 0.8f, 0.8f, 1.0f));
    public static Texture2D Normals => new(new FLinearColor(0.5f, 0.5f, 1.0f, 1.0f));
    public static Texture2D SpecularMasks => new(new FLinearColor(0.5f, 0.0f, 0.5f, 1.0f));
    public static Texture2D Mask => new(new FLinearColor(1.0f, 0.5f, 0.0f, 1.0f));
    public static Texture2D OpacityMask => new(new FLinearColor(1.0f, 1.0f, 1.0f, 1.0f));
    public static Texture2D Empty => new(new FLinearColor(0, 0, 0, 0));
    
    private readonly int Handle;

    public string Name;

    public Texture2D(UTexture2D texture)
    {
        Name = texture.Name;
        var bitmap = texture.Decode()!;
        
        Handle = GL.GenTexture();
        Bind();

        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, bitmap.Width, bitmap.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, bitmap.Data);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat);
    }

    public Texture2D(FLinearColor color)
    {
        Name = color.ToString();
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