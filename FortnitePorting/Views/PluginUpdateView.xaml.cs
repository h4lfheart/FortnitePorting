using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using FortnitePorting.Views.Extensions;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace FortnitePorting.Views;

public partial class PluginUpdateView
{
    private static readonly DirectoryInfo BlenderVersionFolder = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blender Foundation", "Blender"));

    public PluginUpdateView()
    {
        InitializeComponent();
        
        foreach (var folder in BlenderVersionFolder.GetDirectories())
        {
            var isSupported = double.Parse(folder.Name) >= 3.0;
            var extraText = isSupported ? string.Empty : "(Unsupported)";
            var toggleSwitch = new ToggleButton();
            toggleSwitch.Content = $"Blender {folder.Name} {extraText}";
            toggleSwitch.IsEnabled = isSupported;
            toggleSwitch.Tag = folder.Name;
            BlenderInstallationList.Items.Add(toggleSwitch);
        }
    }

    private void OnClickFinished(object sender, RoutedEventArgs e)
    {
        var selectedVersions = BlenderInstallationList.Items
            .OfType<ToggleButton>()
            .Where(x => x.IsChecked.HasValue && x.IsChecked.Value)
            .Select(x => x.Tag.ToString()!).ToArray();
        
        if (selectedVersions.Length == 0) return;
        
        using var addonZip = new ZipArchive(new FileStream("FortnitePortingServer.zip", FileMode.Open));
        foreach (var selectedVersion in selectedVersions)
        {
            var addonPath = Path.Combine(BlenderVersionFolder.FullName, selectedVersion, "scripts", "addons");
            addonZip.ExtractToDirectory(addonPath, overwriteFiles: true);
        }
        
        Close();
        MessageBox.Show($"Successfully updated plugin for Blender {selectedVersions.CommaJoin()}. Please remember to enable the plugin (if this is your first time installing) and restart Blender.", "Updated Plugin Successfully", MessageBoxButton.OK, MessageBoxImage.Information);
    }





}