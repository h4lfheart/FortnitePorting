using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Utils;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Plugin;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Plugin;

public partial class UnrealProjectInfo : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Name))] private string _projectFilePath;
    [ObservableProperty] private Version? _version;
    [JsonIgnore] public Bitmap Image { get; set; } = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/UnrealLogo.png");

    public string Name => ProjectFilePath.SubstringAfterLast("/").SubstringBeforeLast(".");

    public string PluginsFolder => Path.Combine(ProjectFilePath.SubstringBeforeLast("/"), "Plugins");
    public string FortnitePortingFolder => Path.Combine(PluginsFolder, "FortnitePorting");
    public string UEFormatFolder => Path.Combine(PluginsFolder, "UEFormat");
    public string PluginPath => Path.Combine(FortnitePortingFolder, "FortnitePorting.uplugin");

    public UnrealProjectInfo(string projectFilePath)
    {
        ProjectFilePath = projectFilePath;

        Update();
    }

    public void Update()
    {
        if (File.Exists(PluginPath))
        {
            var pluginInfo = JsonConvert.DeserializeObject<UPlugin>(File.ReadAllText(PluginPath));
            Version = new Version(pluginInfo!.VersionName);
        }

        var imageFilePath = Path.Combine(ProjectFilePath.SubstringBeforeLast("/"), $"{Name}.png");
        if (File.Exists(imageFilePath))
        {
            Image = new Bitmap(imageFilePath);
            OnPropertyChanged(nameof(Image));
        }
    }
}