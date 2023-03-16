using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
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

        var diffuseTexture = parameters.GetTextures(CMaterialParams2.Diffuse[0]).FirstOrDefault();
        if (diffuseTexture is not null)
        {
            Diffuse = new Texture2D(diffuseTexture as UTexture2D);
            Diffuse.Bind();
        }
        
        var normalsTexture = parameters.GetTextures(CMaterialParams2.Normals[0]).FirstOrDefault();
        if (normalsTexture is not null)
        {
            Normals = new Texture2D(normalsTexture as UTexture2D);
            Normals.Bind();
        }
        
        var specularMasksTexture = parameters.GetTextures(CMaterialParams2.SpecularMasks[0]).FirstOrDefault();
        if (specularMasksTexture is not null)
        {
            SpecularMasks = new Texture2D(specularMasksTexture as UTexture2D);
            SpecularMasks.Bind();
        }
        
        var maskTexture = parameters.GetTextures(new[] {"M", "Mask", "MaskTexture"}).FirstOrDefault();
        if (maskTexture is not null)
        {
            Mask = new Texture2D(maskTexture as UTexture2D);
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