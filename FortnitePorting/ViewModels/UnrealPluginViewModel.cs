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
    
    public void RemoveProject(UnrealProject project)
    {
        var pluginPath = Path.Combine(project.ProjectFile.DirectoryName, "Plugins");
        Directory.Delete(Path.Combine(pluginPath, "UnrealPSKPSA"), true);
        Directory.Delete(Path.Combine(pluginPath, "FortnitePorting"), true);
        AppSettings.Current.UnrealProjects.Remove(project.ProjectFile.FullName);
        Projects.Remove(project);
    }

    public void Sync(FileInfo uprojectFile)
    {
        App.UnrealPluginStream.Position = 0;
        var pluginZip = ZipFile.Read(App.UnrealPluginStream);
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