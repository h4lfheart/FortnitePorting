using System;
using System.IO;
using System.Windows;
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

    public static readonly DependencyProperty PluginVersionProperty = DependencyProperty.Register(nameof(PluginVersion), typeof(string), typeof(UnrealProject));

    public string PluginVersion
    {
        get => (string) GetValue(PluginVersionProperty);
        set => SetValue(PluginVersionProperty, value);
    }

    public static readonly DependencyProperty PluginVersionColorProperty = DependencyProperty.Register(nameof(PluginVersionColor), typeof(SolidColorBrush), typeof(UnrealProject));

    public SolidColorBrush PluginVersionColor
    {
        get => (SolidColorBrush) GetValue(PluginVersionColorProperty);
        set => SetValue(PluginVersionColorProperty, value);
    }

    public UnrealProject(FileInfo uprojectFile, UPlugin uplugin)
    {
        InitializeComponent();
        DataContext = this;

        ProjectFile = uprojectFile;
        ProjectName = uprojectFile.Name.SubstringBeforeLast(".");
        var imageFile = new FileInfo(Path.Combine(uprojectFile.DirectoryName!, $"{uprojectFile.Name.SubstringBeforeLast(".")}.png"));
        if (imageFile.Exists)
        {
            ProjectImage = new BitmapImage(new Uri(imageFile.FullName, UriKind.Absolute));
        }

        UpdatePluginData(uplugin);
    }

    public void UpdatePluginData(UPlugin uplugin)
    {
        PluginVersion = uplugin.VersionName;
        PluginVersionColor = new SolidColorBrush(uplugin.VersionName.Equals(Globals.VERSION) ? Colors.ForestGreen : Colors.Crimson);
    }
}