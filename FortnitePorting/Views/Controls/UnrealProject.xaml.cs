using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CUE4Parse.Utils;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views.Controls;

public partial class UnrealProject
{
    public FileInfo ProjectFile;
    public ImageSource ProjectImage { get; set; } = new BitmapImage(new Uri($"/FortnitePorting;component/Resources/DefaultUnrealProject.png", UriKind.Relative));
    public string ProjectName { get; set; }
    public string PluginVersion { get; set; }
    public SolidColorBrush PluginVersionColor { get; set; }
    
    public UnrealProject(FileInfo uprojectFile, UPlugin uplugin)
    {
        InitializeComponent();
        DataContext = this;

        ProjectFile = uprojectFile;
        ProjectName = uprojectFile.Name.SubstringBeforeLast(".");
        PluginVersion = uplugin.VersionName;
        PluginVersionColor = new SolidColorBrush(uplugin.VersionName.Equals(Globals.VERSION) ? Colors.ForestGreen : Colors.Crimson);

        var imageFile = new FileInfo(Path.Combine(uprojectFile.DirectoryName!, $"{uprojectFile.Name.SubstringBeforeLast(".")}.png"));
        if (imageFile.Exists)
        {
            ProjectImage = new BitmapImage(new Uri(imageFile.FullName, UriKind.Absolute));
        }
    }
}