using Avalonia.Platform;
using OpenTK.Graphics.OpenGL;
using SkiaSharp;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace FortnitePorting.OpenGL.Rendering.Materials;

public class TextureCube : IDisposable
{
    private readonly int Handle;

    public int Width;
    public int Height;

    public TextureCube(params string[] textures)
    {
        Handle = GL.GenTexture();
        Bind();

        for (uint t = 0; t < textures.Length; t++)
        {
            ProcessPixels(textures[t], TextureTarget.TextureCubeMapPositiveX + t);
        }

        GL.TexParameterf(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameterf(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameterf(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameterf(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameterf(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
    }

    private void ProcessPixels(string texture, TextureTarget target)
    {
        var stream = AssetLoader.Open(new Uri($"avares://FortnitePorting.OpenGL/Assets/Textures/{texture}.png"));
        var image = SKBitmap.Decode(stream);

        Width = image.Width;
        Height = image.Height;

        GL.TexImage2D(target, 0, InternalFormat.Rgba8, Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, image.Bytes);
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