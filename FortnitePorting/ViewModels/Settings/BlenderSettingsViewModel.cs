using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using FortnitePorting.Export;

namespace FortnitePorting.ViewModels.Settings;

public partial class BlenderSettingsViewModel : BaseExportSettings
{
    public bool IsTastyRig => RigType == ERigType.Tasty;
    
    // General
    [ObservableProperty] private bool _scaleDown = true;
    [ObservableProperty] private bool _importIntoCollection = true;
    [ObservableProperty] private bool _importAt3DCursor = false;
    
    // Armature
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsTastyRig))] private ERigType _rigType = ERigType.Default;
    [ObservableProperty] private bool _mergeArmatures = true;
    [ObservableProperty] private bool _reorientBones = false;
    [ObservableProperty] private bool _importSockets = true;
    [ObservableProperty] private bool _importVirtualBones = false;
    [ObservableProperty] private bool _useDynamicBoneShape = true;
    [ObservableProperty] private float _boneLength = 4.0f;
    
    // Mesh
    [ObservableProperty] private int _targetLOD = 0;
    [ObservableProperty] private EPolygonType _polygonType;
    [ObservableProperty] private bool _importCollision = false;
    
    // Material
    [ObservableProperty] private float _ambientOcclusion = 0.0f;
    [ObservableProperty] private float _cavity = 0.0f;
    [ObservableProperty] private float _subsurface = 0.0f;
    [ObservableProperty] private float _toonShadingBrightness = 0.5f;
    
    // Texture
    [ObservableProperty] private ETextureImportMethod _textureImportMethod = ETextureImportMethod.Data;
    
    // Animation
    [ObservableProperty] private bool _loopAnimation = false;
    [ObservableProperty] private bool _updateTimelineLength = false;
    [ObservableProperty] private bool _importSounds = false;
    [ObservableProperty] private bool _importLobbyPoses = false;
    
    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = ELodFormat.AllLods,
            MeshFormat = EMeshFormat.UEFormat,
            AnimFormat = EAnimFormat.UEFormat,
            CompressionFormat = CompressionFormat,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}
public enum ERigType
{
    [Description("Default Rig (FK)")]
    Default,

    [Description("Tasty Rig (IK)")]
    Tasty
}

public enum EPolygonType
{
    [Description("Triangles")]
    Tris,

    [Description("Quads")]
    Quads
}

public enum ETextureImportMethod
{
    [Description("As Texture Data")]
    Data,

    [Description("As Object")]
    Object
}