using System;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.OpenGL.Shaders.Textures;
using OpenTK.Graphics.OpenGL;
using SharpGLTF.Schema2;

namespace FortnitePorting.OpenGL.Shaders;

public class Material : IDisposable
{
    public UMaterialInterface Interface;
    private Texture2D? Diffuse;
    private Texture2D? Normals;
    private Texture2D? SpecularMasks;
    private Texture2D? Mask;

    public Material(UMaterialInterface materialInterface)
    {
        Interface = materialInterface;
        
        var parameters = new CMaterialParams2();
        materialInterface.GetParams(parameters, EMaterialFormat.AllLayers);
        
        if (parameters.TryGetTexture2d(out var diffuse, "Diffuse"))
        {
            Diffuse = new Texture2D(diffuse);
            Diffuse.Bind();
        }
        
        if (parameters.TryGetTexture2d(out var normals, "Normals"))
        {
            Normals = new Texture2D(normals);
            Normals.Bind();
        }
        
        if (parameters.TryGetTexture2d(out var specular, "SpecularMasks"))
        {
            SpecularMasks = new Texture2D(specular);
            SpecularMasks.Bind();
        }
        
        if (parameters.TryGetTexture2d(out var mask, "M"))
        {
            Mask = new Texture2D(mask);
            Mask.Bind(); 
        }
        
    }

    public void Bind()
    {
        Diffuse?.Bind(TextureUnit.Texture0);
        Normals?.Bind(TextureUnit.Texture1);
        SpecularMasks?.Bind(TextureUnit.Texture2);
        Mask?.Bind(TextureUnit.Texture3);
        AppVM.MeshViewer.Renderer.Skybox.Cubemap.Bind(TextureUnit.Texture4);
    }

    public void Dispose()
    {
        Diffuse?.Dispose();
        Normals?.Dispose();
        SpecularMasks?.Dispose();
        Mask?.Dispose();
    }
}