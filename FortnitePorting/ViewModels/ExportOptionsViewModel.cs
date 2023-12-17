using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse_Conversion.UEFormat.Enums;
using FortnitePorting.Framework;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class ExportOptionsViewModel : ViewModelBase
{
    [ObservableProperty] private BlenderExportOptions blender = new();
    [ObservableProperty] private UnrealExportOptions unreal = new();
    [ObservableProperty] private FolderExportOptions folder = new();

    public ExportOptionsBase Get(EExportTargetType type)
    {
        return type switch
        {
            EExportTargetType.Blender => Blender,
            EExportTargetType.Unreal => Unreal,
            EExportTargetType.Folder => Folder
        };
    }

    private static readonly FilePickerFileType LoadFileType = new("Export Options")
    {
        Patterns = new[] { "*.fpexport" }
    };

    private static readonly FilePickerSaveOptions SaveOptions = new()
    {
        Title = "Save Export Options",
        DefaultExtension = ".fpexport",
        FileTypeChoices = new[] { LoadFileType },
        ShowOverwritePrompt = true
    };

    public async Task BrowseSaveFilePath()
    {
        if (await SaveFileDialog(SaveOptions) is { } path)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            await File.WriteAllTextAsync(path, json);
        }
    }

    public async Task BrowseLoadFilePath()
    {
        if (await BrowseFileDialog(LoadFileType) is { } path)
        {
            var imported = JsonConvert.DeserializeObject<ExportOptionsViewModel>(await File.ReadAllTextAsync(path));
            if (imported is null) return;

            Blender = imported.Blender;
            Unreal = imported.Unreal;
            Folder = imported.Folder;
        }
    }

    public void ResetOptions()
    {
        Blender = new BlenderExportOptions();
        Unreal = new UnrealExportOptions();
        Folder = new FolderExportOptions();
    }
}

public partial class ExportOptionsBase : ObservableObject
{
    [ObservableProperty] private EMeshExportTypes meshFormat = EMeshExportTypes.UEFormat;
    [ObservableProperty] private EAnimExportTypes animFormat = EAnimExportTypes.UEFormat;
    [ObservableProperty] private EFileCompressionFormat compressionFormat = EFileCompressionFormat.ZSTD;

    [ObservableProperty] private bool exportMaterials = true;
    [ObservableProperty] private EImageType imageType = EImageType.PNG;


    public virtual ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions();
    }
}

public partial class BlenderExportOptions : ExportOptionsBase
{
    [ObservableProperty] private bool scaleDown = true;
    [ObservableProperty] private bool importCollection = true;

    [ObservableProperty] private ERigType rigType = ERigType.Default;
    [ObservableProperty] private bool mergeSkeletons = true;
    [ObservableProperty] private bool reorientBones = false;
    [ObservableProperty] private bool hideFaceBones = false;
    [ObservableProperty] private float boneSize = 4f;

    [ObservableProperty] private ESupportedLODs levelOfDetail = ESupportedLODs.LOD0;
    [ObservableProperty] private bool useQuads = false;
    [ObservableProperty] private bool preserveVolume = false;

    [ObservableProperty] private float ambientOcclusion = 0.0f;
    [ObservableProperty] private float cavity = 0.0f;
    [ObservableProperty] private float subsurface = 0.0f;
    [ObservableProperty] private float toonBrightness = 1.0f;

    [ObservableProperty] private bool importSounds = true;
    [ObservableProperty] private bool loopAnimation = true;
    [ObservableProperty] private bool updateTimelineLength = true;

    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = LevelOfDetail is ESupportedLODs.LOD0 ? ELodFormat.FirstLod : ELodFormat.AllLods,
            MeshFormat = EMeshFormat.UEFormat,
            AnimFormat = EAnimFormat.UEFormat,
            CompressionFormat = CompressionFormat,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}

public partial class UnrealExportOptions : ExportOptionsBase
{
    [ObservableProperty] private ESupportedLODs levelOfDetail = ESupportedLODs.LOD0;

    [ObservableProperty] private bool forUEFN = true;
    [ObservableProperty] private float ambientOcclusion = 0.0f;
    [ObservableProperty] private float cavity = 0.0f;
    [ObservableProperty] private float subsurface = 0.0f;

    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = LevelOfDetail is ESupportedLODs.LOD0 ? ELodFormat.FirstLod : ELodFormat.AllLods,
            MeshFormat = EMeshFormat.UEFormat,
            AnimFormat = EAnimFormat.UEFormat,
            CompressionFormat = CompressionFormat,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}

public partial class FolderExportOptions : ExportOptionsBase
{
    [ObservableProperty] private ELodFormat lodFormat = ELodFormat.FirstLod;

    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = LodFormat,
            MeshFormat = MeshFormat switch
            {
                EMeshExportTypes.UEFormat => EMeshFormat.UEFormat,
                EMeshExportTypes.ActorX => EMeshFormat.ActorX
            },
            AnimFormat = AnimFormat switch
            {
                EAnimExportTypes.UEFormat => EAnimFormat.UEFormat,
                EAnimExportTypes.ActorX => EAnimFormat.ActorX
            },
            CompressionFormat = CompressionFormat,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}