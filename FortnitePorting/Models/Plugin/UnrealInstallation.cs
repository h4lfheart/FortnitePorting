using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Plugin;

public partial class UnrealInstallation : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Name))]
    private string _projectFilePath;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(VersionString))] [field: JsonIgnore]
    [property: JsonIgnore]
    private Version? _version;

    [JsonIgnore]
    public string VersionString => Version is null ? string.Empty : $"v{Version}";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(StatusBrush))]
    [property: JsonIgnore]
    private EPluginStatusType _status = EPluginStatusType.Modifying;

    [JsonIgnore]
    public SolidColorBrush StatusBrush => Status switch
    {
        EPluginStatusType.Newest => SolidColorBrush.Parse("#17854F"),
        EPluginStatusType.UpdateAvailable => SolidColorBrush.Parse("#E0A100"),
        EPluginStatusType.Failed => SolidColorBrush.Parse("#A61717"),
        EPluginStatusType.Modifying => SolidColorBrush.Parse("#6F6F75")
    };

    [JsonIgnore]
    public Bitmap Image { get; private set; } =
        ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/UnrealLogo.png");

    [JsonIgnore]
    public string Name => ProjectFilePath.SubstringAfterLast("/").SubstringBeforeLast(".");

    [JsonIgnore]
    public string PluginsFolder => Path.Combine(ProjectFilePath.SubstringBeforeLast("/"), "Plugins");
    
    [JsonIgnore]
    public string FortnitePortingFolder => Path.Combine(PluginsFolder, "FortnitePorting");
    
    [JsonIgnore]
    public string UEFormatFolder => Path.Combine(PluginsFolder, "UEFormat");
    
    [JsonIgnore]
    public string PluginPath => Path.Combine(FortnitePortingFolder, "FortnitePorting.uplugin");

    public static readonly DirectoryInfo PluginWorkingDirectory =
        new(Path.Combine(App.PluginsFolder.FullName, "Unreal"));

    public UnrealInstallation(string projectFilePath)
    {
        ProjectFilePath = projectFilePath;
    }

    public bool SyncVersion()
    {
        if (!File.Exists(PluginPath))
        {
            Info.Message("Unreal Plugin",
                $"Plugin file does not exist at path {PluginPath}, installation may have gone wrong.\nPlease remove the project from Fortnite Porting and try again.");
            Status = EPluginStatusType.Failed;
            return false;
        }

        var pluginInfo = JsonConvert.DeserializeObject<UPlugin>(File.ReadAllText(PluginPath));
        Version = new Version(pluginInfo!.VersionName);

        var fpPluginVersion = new FPVersion(Version.Major, Version.Minor, Version.Build);
        Status = fpPluginVersion.Equals(Globals.Version)
            ? EPluginStatusType.Newest
            : EPluginStatusType.UpdateAvailable;

        return true;
    }

    public void Install(bool verbose = true)
    {
        Status = EPluginStatusType.Modifying;

        MiscExtensions.Copy(PluginWorkingDirectory.FullName, PluginsFolder);

        var didSyncProperly = SyncVersion();
        if (verbose && !didSyncProperly)
        {
            Info.Message("Plugin Installation Failed",
                "Failed to install the plugin, please install it manually.",
                useButton: true, buttonTitle: "Open Plugins Folder",
                buttonCommand: () => App.Launch(App.PluginsFolder.FullName));

            Status = EPluginStatusType.Failed;
            return;
        }

        RefreshImage();
        Status = EPluginStatusType.Newest;
    }

    public void Uninstall()
    {
        Status = EPluginStatusType.Modifying;

        if (Directory.Exists(FortnitePortingFolder))
            Directory.Delete(FortnitePortingFolder, true);

        if (Directory.Exists(UEFormatFolder))
            Directory.Delete(UEFormatFolder, true);
    }

    private void RefreshImage()
    {
        var imageFilePath = Path.Combine(ProjectFilePath.SubstringBeforeLast("/"), $"{Name}.png");
        if (!File.Exists(imageFilePath)) return;

        Image = new Bitmap(imageFilePath);
        OnPropertyChanged(nameof(Image));
    }
    
    public async Task Launch()
    {
        App.Launch(ProjectFilePath);
    }
}

public class UPlugin
{
    public string VersionName;
}