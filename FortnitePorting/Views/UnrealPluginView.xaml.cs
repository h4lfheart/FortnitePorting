using System.Windows;
using FortnitePorting.AppUtils;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class UnrealPluginView
{
    public UnrealPluginView()
    {
        InitializeComponent();
        AppVM.UnrealVM = new UnrealPluginViewModel();
        DataContext = AppVM.UnrealVM;

        AppVM.UnrealVM.Initialize();
    }

    private void OnClickAddProject(object sender, RoutedEventArgs e)
    {
        if (AppHelper.TrySelectFile(out var path, filter: "Unreal Project|*.uproject"))
        {
            AppVM.UnrealVM.AddProject(path);
        }
    }

    private void OnClickSync(object sender, RoutedEventArgs e)
    {
        foreach (var project in AppVM.UnrealVM.Projects)
        {
            AppVM.UnrealVM.Sync(project.ProjectFile);

            if (AppVM.UnrealVM.TryGetPluginData(project.ProjectFile, out var plugin))
            {
                project.UpdatePluginData(plugin);
            }
        }
    }
}