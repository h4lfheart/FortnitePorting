using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using FortnitePorting.RenderingX.Data.Programs;
using FortnitePorting.RenderingX.Data.Textures;
using FortnitePorting.RenderingX.Extensions;
using Serilog;

namespace FortnitePorting.RenderingX.Materials;


public class Material
{
    public Texture2D?[] Diffuse = new Texture2D[4];
    public Texture2D?[] Normals = new Texture2D[4];
    public Texture2D?[] SpecularMasks = new Texture2D[4];
    public bool UseLayers;
    public int MaxLayer;

    public static readonly Lazy<Texture2D> DefaultDiffuse = new(() =>
    {
        var texture = new Texture2D(1, 1, [178, 178, 178, 255]);
        texture.Generate();
        return texture;
    });
    
    public static readonly Lazy<Texture2D> DefaultNormal = new(() =>
    {
        var texture = new Texture2D(1, 1, [255, 128, 128, 255]);
        texture.Generate();
        return texture;
    });
    
    public static readonly Lazy<Texture2D> DefaultSpecular = new(() =>
    {
        var texture = new Texture2D(1, 1, [0, 0, 255, 255]);
        texture.Generate();
        return texture;
    });

    private static readonly LayeredMaterialMappings DiffuseMappings = new()
    {
        Layers = [
            new MaterialMappings([
                "Diffuse",
                "D",
                "Base Color",
                "BaseColor",
                "Concrete",
                "Trunk_BaseColor",
                "Diffuse Top",
                "BaseColor_Trunk",
                "CliffTexture",
                "PM_Diffuse",
                "___Diffuse",
                "Background Diffuse",
                "BG Diffuse Texture"
            ]),
            new MaterialMappings(["Diffuse_Texture_2"]),
            new MaterialMappings(["Diffuse_Texture_3"]),
            new MaterialMappings(["Diffuse_Texture_4"]),
            
        ]
    };

    private static readonly LayeredMaterialMappings NormalMappings = new()
    {
        Layers = [
            new MaterialMappings([
                "Normals",
                "N",
                "Normal",
                "NormalMap",
                "ConcreteTextureNormal",
                "Trunk_Normal",
                "Normals Top",
                "Normal_Trunk",
                "CliffNormal",
                "PM_Normals",
                "_Normal"
            ]),
            new MaterialMappings(["Normals_Texture_2"]),
            new MaterialMappings(["Normals_Texture_3"]),
            new MaterialMappings(["Normals_Texture_4"]),
        ]
    };


    private static readonly LayeredMaterialMappings SpecularMappings = new()
    {
        Layers = [
            new MaterialMappings([
                "SpecularMasks",
                "S",
                "SRM",
                "S Mask",
                "Specular Mask",
                "SpecularMask",
                "Concrete_SpecMask",
                "Trunk_Specular",
                "Specular Top",
                "SMR_Trunk",
                "Cliff Spec Texture",
                "PM_SpecularMasks",
                "__PBR Masks"
            ]),
            new MaterialMappings(["SpecularMasks_2"]),
            new MaterialMappings(["SpecularMasks_3"]),
            new MaterialMappings(["SpecularMasks_4"]),
        ]
    };
    
    private static readonly string[] UseLayerNames =
    [
        "Use 2 Layers", "Use 3 Layers", "Use 4 Layers", "Use 5 Layers", "Use 6 Layers", "Use 7 Layers",
        "Use 2 Materials", "Use 3 Materials", "Use 4 Materials", "Use 5 Materials", "Use 6 Materials", "Use 7 Materials",
        "Use_Multiple_Material_Textures"
    ];

    public Material()
    {
        FillWithDefaults(Diffuse, DefaultDiffuse.Value);
        FillWithDefaults(Normals, DefaultNormal.Value);
        FillWithDefaults(SpecularMasks, DefaultSpecular.Value);    
    }
    
    public Material(UMaterialInstanceConstant materialInstance)
    {
        foreach (var textureParameter in materialInstance.TextureParameterValues)
        {
            DiffuseMappings.TrySetTexture(Diffuse, textureParameter);
            NormalMappings.TrySetTexture(Normals, textureParameter);
            SpecularMappings.TrySetTexture(SpecularMasks, textureParameter);
        }

        CheckLayerUsage(materialInstance);

        FillWithDefaults(Diffuse, DefaultDiffuse.Value);
        FillWithDefaults(Normals, DefaultNormal.Value);
        FillWithDefaults(SpecularMasks, DefaultSpecular.Value); 
    }

    public Material(UMaterial material)
    {
        FillWithDefaults(Diffuse, DefaultDiffuse.Value);
        FillWithDefaults(Normals, DefaultNormal.Value);
        FillWithDefaults(SpecularMasks, DefaultSpecular.Value); 
    }
    
    public void SetUniforms(ShaderProgram shader)
    {
        var unitOffset = 0;
        void SetTextureUnforms(string name, Texture2D?[] textures)
        {
            for (var textureIndex = 0; textureIndex < textures.Length; textureIndex++)
            {
                var texture = textures[textureIndex];
                if (texture is null) continue;
                
                shader.SetUniform($"{name}{textureIndex}", unitOffset);
                unitOffset++;
            }
        }
        
        SetTextureUnforms("diffuse", Diffuse);
        SetTextureUnforms("normal", Normals);
        SetTextureUnforms("specular", SpecularMasks);
        
        shader.SetUniform("useLayers", UseLayers ? 1 : 0);
        shader.SetUniform("maxLayer", MaxLayer);
    }
    
    public void Bind()
    {
        var unitOffset = 0;
        void BindTextureArray(Texture2D?[] textures)
        {
            foreach (var texture in textures)
            {
                if (texture is null) continue;
                
                texture.Bind((TextureUnit) ((int) TextureUnit.Texture0 + unitOffset));
                unitOffset++;
            }
        }

        BindTextureArray(Diffuse);
        BindTextureArray(Normals);
        BindTextureArray(SpecularMasks);
    }

    private void CheckLayerUsage(UUnrealMaterial? material)
    {
        if (material is not UMaterialInstanceConstant materialInstance) return;
        
        SetLayerParameters(materialInstance.StaticParameters);
        
        if (materialInstance.TryLoadEditorData<UMaterialInstanceEditorOnlyData>(out var materialInstanceEditorData) &&
            materialInstanceEditorData?.StaticParameters is not null)
        {
            SetLayerParameters(materialInstanceEditorData.StaticParameters);
        }

        CheckLayerUsage(materialInstance.Parent);
    }

    private void SetLayerParameters(FStaticParameterSet? staticParameterSet)
    {
        if (staticParameterSet is null)
            return;
        
        UseLayers |= staticParameterSet.StaticSwitchParameters.Any(param => UseLayerNames.Contains(param.Name, StringComparer.OrdinalIgnoreCase) && param.Value);
        
        bool HasLayerToggle(params string[] keys)
        {
            return keys.Any(key => staticParameterSet.StaticSwitchParameters.Any(param => param.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && param.Value));
        }

        MaxLayer = 4;
        if (HasLayerToggle("Use 2 Materials", "Use 2 Layers"))
        {
            MaxLayer = 2;
        } 
        
        if (HasLayerToggle("Use 3 Materials", "Use 3 Layers"))
        {
            MaxLayer = 3;
        }
        
        if (HasLayerToggle("Use 4 Materials", "Use 4 Layers"))
        {
            MaxLayer = 4;
        }
    }

    private void FillWithDefaults(Texture2D?[] textures, Texture2D defaultTexture)
    {
        for (var textureIndex = 0; textureIndex < textures.Length; textureIndex++)
        {
            textures[textureIndex] ??= defaultTexture;
        }
    }
}