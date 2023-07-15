using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using Ionic.Zip;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class UnrealPluginViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<UnrealProject> projects = new();

    private const string UPluginPath = "Plugins/FortnitePorting/FortnitePorting.uplugin";

    public void Initialize()
    {
        AppSettings.Current.UnrealProjects.ForEach(AddProject);
    }

    public void AddProject(string path)
    {
        var uprojectFile = new FileInfo(path);
        if (!uprojectFile.Exists) return;

        if (TryGetPluginData(uprojectFile, out var plugin))
        {
            AppSettings.Current.UnrealProjects.AddUnique(uprojectFile.FullName);
            Projects.Add(new UnrealProject(uprojectFile, plugin));
        }
    }

    public void Sync(FileInfo uprojectFile)
    {
        var zipStream = Application.GetResourceStream(new Uri($"/FortnitePorting;component/Plugin/FortnitePortingUnreal.zip", UriKind.Relative))?.Stream;
        if (zipStream is null) return;

        var pluginZip = ZipFile.Read(zipStream);
        pluginZip.ExtractAll(Path.Combine(uprojectFile.DirectoryName, "Plugins"), ExtractExistingFileAction.OverwriteSilently);
    }

    public bool TryGetPluginData(FileInfo uprojectFile, out UPlugin plugin)
    {
        var upluginFile = new FileInfo(Path.Combine(uprojectFile.DirectoryName!, UPluginPath));
        plugin = upluginFile.Exists ? JsonConvert.DeserializeObject<UPlugin>(File.ReadAllText(upluginFile.FullName)) : UPlugin.Invalid;
        return plugin is not null;
    }
}

public class UPlugin
{
    public string VersionName;
    public bool IsBetaVersion;

    public static UPlugin Invalid = new()
    {
        VersionName = "???"
    };
}