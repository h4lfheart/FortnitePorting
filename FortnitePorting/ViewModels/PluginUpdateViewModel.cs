using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Views;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.ViewModels;

public partial class PluginUpdateViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ToggleButton> blenderInstallations = new();
    [ObservableProperty] private string addVersion;

    public void Initialize()
    {
        var normalBlenderInstall = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blender Foundation", "Blender"));
        if (normalBlenderInstall.Exists)
        {
            foreach (var folder in normalBlenderInstall.GetDirectories())
            {
                AddInstallation(folder);
            }
        }

        var steamApps = SteamDetection.GetSteamApps(SteamDetection.GetSteamLibs());
        var steamBlender = steamApps.FirstOrDefault(x => x.Name.Contains("Blender", StringComparison.OrdinalIgnoreCase));
        if (steamBlender is not null)
        {
            var steamBlenderInstall = new DirectoryInfo(steamBlender.GameRoot);
            foreach (var folder in steamBlenderInstall.GetDirectories())
            {
                AddInstallation(folder, prefix: "Steam");
            }
        }
    }

    private void AddInstallation(DirectoryInfo directory, string prefix = "")
    {
        if (!TryParseDouble(directory.Name, out var numberVersion)) return;
        numberVersion = numberVersion.Truncate(1);

        var addonsPath = Path.Combine(directory.FullName, "scripts", "addons");
        Directory.CreateDirectory(addonsPath);

        Log.Information("Found Blender installation at {0}.", directory.FullName);
        var isSupported = numberVersion >= 3.0;
        var extraText = isSupported ? string.Empty : "(Unsupported)";

        if (!string.IsNullOrWhiteSpace(prefix)) prefix += " ";

        var toggleSwitch = new ToggleButton
        {
            Content = $"{prefix}Blender {numberVersion:0.0} {extraText}",
            IsEnabled = isSupported,
            Tag = directory
        };
        BlenderInstallations.Add(toggleSwitch);
    }

    private bool TryParseDouble(string input, out double value)
    {
        var worked = double.TryParse(input, NumberStyles.Any, new NumberFormatInfo { NumberDecimalSeparator = "." }, out value);
        return worked;
    }

    [RelayCommand]
    public void AddCustomVersion()
    {
        var isValid = TryParseDouble(AddVersion, out var numberValue);
        var numberString = $"{numberValue.Truncate(1):0.0}";
        if (isValid && !BlenderInstallations.Any(x => (x.Tag as DirectoryInfo)!.Name.Equals(numberString)))
        {
            var targetInstallPath = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Blender Foundation", "Blender", numberString));
            targetInstallPath.Create();

            AddInstallation(targetInstallPath);
        }

        AddVersion = string.Empty;
    }
}