using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.OpenGL.Rendering.Levels;
using FortnitePorting.Shared.Extensions;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.OpenGL.Rendering.Materials;

public class Material : IDisposable
{
    public UMaterialInterface Interface;
    
    private Texture2D?[] Diffuse = new Texture2D[4];
    private Texture2D?[] Normals = new Texture2D[4];
    private Texture2D?[] SpecularMasks = new Texture2D[4];
    private Texture2D?[] Mask = new Texture2D[4];
    private Texture2D?[] OpacityMask = new Texture2D[4];

    private static readonly LayeredMaterialMappings DiffuseMappings = new()
    {
        Layer0 =
        [
            "Diffuse",
            "D",
            "Base Color",
            "Concrete",
            "Trunk_BaseColor",
            "BaseColor_Trunk",
            "Diffuse Top"
        ],
        Layer1 = ["Diffuse_Texture_2"],
        Layer2 = ["Diffuse_Texture_3"],
        Layer3 = ["Diffuse_Texture_4"],
        Layer4 = ["Diffuse_Texture_5"],
        Layer5 = ["Diffuse_Texture_6"],
    };
    
    private static readonly LayeredMaterialMappings NormalMappings = new()
    {
        Layer0 =
        [
            "Normals",
            "N",
            "Normal",
            "NormalMap",
            "ConcreteTextureNormal",
            "Trunk_Normal",
            "Normal_Trunk",
            "Normals Top"
        ],
        Layer1 = ["Normals_Texture_2"],
        Layer2 = ["Normals_Texture_3"],
        Layer3 = ["Normals_Texture_4"],
        Layer4 = ["Normals_Texture_5"],
        Layer5 = ["Normals_Texture_6"],
    };
    
    private static readonly LayeredMaterialMappings SpecularMappings = new()
    {
        Layer0 =
        [
            "SpecularMasks",
            "S",
            "SRM",
            "Specular Mask",
            "Concrete_SpecMask",
            "Trunk_Specular",
            "SRM_Trunk",
            "Specular Top"
        ],
        Layer1 = ["SpecularMasks_2"],
        Layer2 = ["SpecularMasks_3"],
        Layer3 = ["SpecularMasks_4"],
        Layer4 = ["SpecularMasks_5"],
        Layer5 = ["SpecularMasks_6"],
    };
    
    private static readonly LayeredMaterialMappings OpacityMappings = new()
    {
        Layer0 =
        [
            "MaskTexture",
            "OpacityMask"
        ],
        Layer1 = ["MaskTexture_2"],
        Layer2 = ["MaskTexture_3"],
        Layer3 = ["MaskTexture_4"],
        Layer4 = ["MaskTexture_5"],
        Layer5 = ["MaskTexture_6"],
    };

    private static readonly string[] UseLayerNames =
    [
        "Use 2 Layers", "Use 3 Layers", "Use 4 Layers", "Use 5 Layers", "Use 6 Layers", "Use 7 Layers",
        "Use 2 Materials", "Use 3 Materials", "Use 4 Materials", "Use 5 Materials", "Use 6 Materials", "Use 7 Materials",
        "Use_Multiple_Material_Textures"
    ];

    public bool UseLayers;

    public Material(UMaterialInterface materialInterface, TextureData? textureData = null)
    {
        Interface = materialInterface;
        
        if (textureData is not null)
        {
            if (textureData.Diffuse is not null)
                Diffuse[0] = new Texture2D(textureData.Diffuse);
            
            if (textureData.Normal is not null)
                Normals[0] = new Texture2D(textureData.Normal);
            
            if (textureData.Specular is not null)
                SpecularMasks[0] = new Texture2D(textureData.Specular);
        }
        
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            AccumulateParameters(materialInstance);

            UseLayers = materialInstance.StaticParameters?.StaticSwitchParameters.Any(param => UseLayerNames.Contains(param.Name, StringComparer.OrdinalIgnoreCase) && param.Value) ?? false;
            
            // no alpha
            if (materialInstance.StaticParameters?.StaticSwitchParameters.Any(param => param.Name.Equals("IsTrunk") && param.Value) ?? false)
            {
                OpacityMask[0] = Texture2D.OpacityMask;
            }
        }
        
        if (materialInterface.Name.Contains("Decal", StringComparison.OrdinalIgnoreCase))
        {
            OpacityMask[0] = Texture2D.Empty;
        }
        
        
        FillWithDefaults(Diffuse, Texture2D.Diffuse);
        FillWithDefaults(Normals, Texture2D.Normals);
        FillWithDefaults(SpecularMasks, Texture2D.SpecularMasks);
        FillWithDefaults(Mask, Texture2D.Mask);
        FillWithDefaults(OpacityMask, Texture2D.OpacityMask);
    }

    public void AccumulateParameters(UMaterialInstanceConstant materialInstance)
    {
        foreach (var textureParameter in materialInstance.TextureParameterValues)
        {
            DiffuseMappings.TryAssign(textureParameter, Diffuse);
            NormalMappings.TryAssign(textureParameter, Normals);
            SpecularMappings.TryAssign(textureParameter, SpecularMasks);
            OpacityMappings.TryAssign(textureParameter, OpacityMask);
        }
        
        if (materialInstance.Parent is UMaterialInstanceConstant parentMaterial) AccumulateParameters(parentMaterial);
    }

    public void Render(Shader shader)
    {
        shader.SetUniform("parameters.useLayers", UseLayers ? 1 : 0);
        
        var unit = 0;
        for (var i = 0; i < Diffuse.Length; i++)
        {
            if (Diffuse[i] is null) continue;
            shader.SetUniform($"parameters.diffuse[{i}]", unit);
            Diffuse[i].Bind((TextureUnit) ((int) TextureUnit.Texture0 + unit));
            unit++;
        }
        
        for (var i = 0; i < Normals.Length; i++)
        {
            if (Normals[i] is null) continue;
            shader.SetUniform($"parameters.normal[{i}]", unit);
            Normals[i].Bind((TextureUnit) ((int) TextureUnit.Texture0 + unit));
            unit++;
        }
        
        for (var i = 0; i < SpecularMasks.Length; i++)
        {
            if (SpecularMasks[i] is null) continue;
            shader.SetUniform($"parameters.specular[{i}]", unit);
            SpecularMasks[i].Bind((TextureUnit) ((int) TextureUnit.Texture0 + unit));
            unit++;
        }
        
        for (var i = 0; i < Mask.Length; i++)
        {
            if (Mask[i] is null) continue;
            shader.SetUniform($"parameters.mask[{i}]", unit);
            Mask[i].Bind((TextureUnit) ((int) TextureUnit.Texture0 + unit));
            unit++;
        }

        for (var i = 0; i < OpacityMask.Length; i++)
        {
            if (OpacityMask[i] is null) continue;
            shader.SetUniform($"parameters.opacityMask[{i}]", unit);
            OpacityMask[i].Bind((TextureUnit) ((int) TextureUnit.Texture0 + unit));
            unit++;
        }

    }

    public void FillWithDefaults(Texture2D?[] textures, Texture2D def)
    {
        for (var i = 0; i < textures.Length; i++)
        {
            textures[i] ??= def;
        }
    }

    public void Dispose()
    {
        Diffuse.ForEach(x => x.Dispose());
        Normals.ForEach(x => x.Dispose());
        SpecularMasks.ForEach(x => x.Dispose());
        Mask.ForEach(x => x.Dispose());
        OpacityMask.ForEach(x => x.Dispose());
    }
}

public class LayeredMaterialMappings
{
    public string[] Layer0;
    public string[] Layer1;
    public string[] Layer2;
    public string[] Layer3;
    public string[] Layer4;
    public string[] Layer5;

    public void TryAssign(FTextureParameterValue parameter, Texture2D?[] textures)
    {
        if (parameter.ParameterValue.IsNull) return;
        
        var name = parameter.Name;
        if (Layer0.Contains(name))
        {
            textures[0] ??= new Texture2D(parameter.ParameterValue.Load<UTexture2D>()!);
        }
        else if (Layer1.Contains(name))
        {
            textures[1] ??= new Texture2D(parameter.ParameterValue.Load<UTexture2D>()!);
        }
        else if (Layer2.Contains(name))
        {
            textures[2] ??= new Texture2D(parameter.ParameterValue.Load<UTexture2D>()!);
        }
        else if (Layer3.Contains(name))
        {
            textures[3] ??= new Texture2D(parameter.ParameterValue.Load<UTexture2D>()!);
        }
        else if (Layer4.Contains(name))
        {
            textures[4] ??= new Texture2D(parameter.ParameterValue.Load<UTexture2D>()!);
        }
        else if (Layer5.Contains(name))
        {
            textures[5] ??= new Texture2D(parameter.ParameterValue.Load<UTexture2D>()!);
        }
    }
}