using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using Ionic.Zip;
using MercuryCommons.Utilities.Extensions;
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
        
        var upluginFile = new FileInfo(Path.Combine(uprojectFile.DirectoryName!, UPluginPath));
        if (!upluginFile.Exists) Sync(uprojectFile);
        
        var pluginData = JsonConvert.DeserializeObject<UPlugin>(File.ReadAllText(upluginFile.FullName));
        if (pluginData is null) return;
        
        AppSettings.Current.UnrealProjects.AddUnique(uprojectFile.FullName);
        Projects.Add(new UnrealProject(uprojectFile, pluginData));
    }

    public void Sync(FileInfo uprojectFile)
    {
        var zipStream = Application.GetResourceStream(new Uri($"/FortnitePorting;component/Plugin/FortnitePortingUnreal.zip", UriKind.Relative))?.Stream;
        if (zipStream is null) return;
        
        var pluginZip = ZipFile.Read(zipStream);
        pluginZip.ExtractAll(Path.Combine(uprojectFile.DirectoryName, "Plugins"), ExtractExistingFileAction.OverwriteSilently);
    }
}

public class UPlugin
{
    public Version Version => new(VersionName);
    public string VersionName;
    public bool IsBetaVersion;
}