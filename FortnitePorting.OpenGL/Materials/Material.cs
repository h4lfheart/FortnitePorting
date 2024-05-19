using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.Core;
using FortnitePorting.OpenGL.Rendering.Levels;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Materials;

public class Material : IDisposable
{
    public UMaterialInterface Interface;
    
    private Texture2D? Diffuse;
    private Texture2D? Normals;
    private Texture2D? SpecularMasks;
    private Texture2D? Mask;
    private Texture2D? OpacityMask;

    private static readonly string[] DiffuseNames = 
    [
        "Diffuse",
        "D",
        "Base Color",
        "Concrete",
        "Trunk_BaseColor",
        "BaseColor_Trunk",
        "Diffuse Top"
    ];
    
    private static readonly string[] NormalNames = 
    [
        "Normals",
        "N",
        "Normal",
        "NormalMap",
        "ConcreteTextureNormal",
        "Trunk_Normal",
        "Normal_Trunk",
        "Normals Top"
    ];
    
    private static readonly string[] SpecularNames = 
    [
        "SpecularMasks",
        "S",
        "SRM",
        "Specular Mask",
        "Concrete_SpecMask",
        "Trunk_Specular",
        "SRM_Trunk",
        "Specular Top"
    ];

    private static readonly string[] OpacityMaskNames =
    [
        "MaskTexture",
        "OpacityMask"
    ];

    public Material(UMaterialInterface materialInterface, TextureData? textureData = null)
    {
        Interface = materialInterface;

        if (textureData is not null)
        {
            if (textureData.Diffuse is not null)
                Diffuse = new Texture2D(textureData.Diffuse);
            
            if (textureData.Normal is not null)
                Normals = new Texture2D(textureData.Normal);
            
            if (textureData.Specular is not null)
                SpecularMasks = new Texture2D(textureData.Specular);
        }
        
        if (materialInterface is not UMaterialInstanceConstant materialInstance) return;

        AccumulateParameters(materialInstance);
        
        Diffuse ??= Texture2D.Diffuse;
        Normals ??= Texture2D.Normals;
        SpecularMasks ??= Texture2D.SpecularMasks;
        Mask ??= Texture2D.Mask;
        OpacityMask ??= Texture2D.OpacityMask;

        // no alpha
        if (materialInstance.StaticParameters?.StaticSwitchParameters.Any(param => param.Name.Equals("IsTrunk") && param.Value) ?? false)
        {
            OpacityMask = Texture2D.OpacityMask;
        }
    }

    public void AccumulateParameters(UMaterialInstanceConstant materialInstance)
    {
        foreach (var textureParameter in materialInstance.TextureParameterValues)
        {
            var name = textureParameter.Name;
            if (DiffuseNames.Contains(name))
            {
                Diffuse ??= new Texture2D(textureParameter.ParameterValue.Load<UTexture2D>()!);
            }
            else if (NormalNames.Contains(name))
            {
                Normals ??= new Texture2D(textureParameter.ParameterValue.Load<UTexture2D>()!);
            }
            else if (OpacityMaskNames.Contains(name))
            {
                OpacityMask ??= new Texture2D(textureParameter.ParameterValue.Load<UTexture2D>()!);
            }
        }
        
        if (materialInstance.Parent is UMaterialInstanceConstant parentMaterial) AccumulateParameters(parentMaterial);
    }

    public void Bind()
    {
        Diffuse?.Bind(TextureUnit.Texture0);
        Normals?.Bind(TextureUnit.Texture1);
        SpecularMasks?.Bind(TextureUnit.Texture2);
        Mask?.Bind(TextureUnit.Texture3);
        OpacityMask?.Bind(TextureUnit.Texture4);
    }

    public void Dispose()
    {
        Diffuse?.Dispose();
        Normals?.Dispose();
        SpecularMasks?.Dispose();
        Mask?.Dispose();
        OpacityMask?.Dispose();
    }
}