using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Plugin;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Plugin;

public partial class UnrealPluginViewModel : ViewModelBase
{
    [ObservableProperty] private bool _automaticallySync = true;
    [ObservableProperty] private ObservableCollection<UnrealProjectInfo> _projects = [];
    [ObservableProperty, JsonIgnore] private int _selectedProjectIndex = 0;
    
    private static readonly DirectoryInfo UnrealRoot = new(Path.Combine(App.PluginsFolder.FullName, "Unreal"));

    public override async Task Initialize()
    {
        UnrealRoot.Create();
    }

    public async Task AddProject()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.UnrealProjectFileType) is not { } projectPath) return;

        var project = new UnrealProjectInfo(projectPath);
        Projects.Add(project);

        await Sync(project);
    }

    public async Task RemoveProject()
    {
        if (Projects.Count == 0) return;

        await TaskService.RunAsync(() =>
        {
            var installation = Projects[SelectedProjectIndex];
            
            Directory.Delete(installation.FortnitePortingFolder, true);
            Directory.Delete(installation.UEFormatFolder, true);

            var selectedIndexToRemove = SelectedProjectIndex;
            Projects.RemoveAt(selectedIndexToRemove);
            SelectedProjectIndex = selectedIndexToRemove == 0 ? 0 : selectedIndexToRemove - 1;
        });
    }

    public async Task SyncProjects()
    {
        await SyncProjects(true);
    }
    
    public async Task SyncProjects(bool verbose)
    {
        var currentVersion = Globals.Version.ToVersion();
        foreach (var project in Projects)
        {
            if (currentVersion == project.Version)
            {
                if (verbose)
                {
                    Info.Message("Unreal Plugin", $"{project.Name} is already up to date.");
                }
                
                continue;
            }

            var previousVersion = project.Version;
            await Sync(project);

            Info.Message("Unreal Plugin", $"Successfully updated the {project.Name} plugin from {previousVersion} to {currentVersion}");
        }
    }

    public async Task Sync(UnrealProjectInfo unrealProjectInfo)
    {
        MiscExtensions.Copy(Path.Combine(App.PluginsFolder.FullName, "Unreal"), unrealProjectInfo.PluginsFolder);
        unrealProjectInfo.Update();
    }
}

public class UPlugin
{
    public string VersionName;
}