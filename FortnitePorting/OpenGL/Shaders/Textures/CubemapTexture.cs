using System;
using System.Windows;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FortnitePorting.OpenGL.Shaders.Textures;

public class CubemapTexture : IDisposable
{
    private readonly TextureHandle Handle;

    public int Width;
    public int Height;

    public CubemapTexture(params string[] textures)
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
        var imageResource = Application.GetResourceStream(new Uri($"/FortnitePorting;component/Resources/Shaders/{texture}.png", UriKind.Relative));
        if (imageResource is null) return;

        var image = Image.Load<Rgba32>(imageResource.Stream);
        if (image is null) return;

        Width = image.Width;
        Height = image.Height;

        var imageBytes = new byte[image.Width * image.Height * 32];
        image.CopyPixelDataTo(imageBytes);

        GL.TexImage2D(target, 0, InternalFormat.Rgba8, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, imageBytes);
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