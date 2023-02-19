using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using FortnitePorting.Views.Extensions;
using Microsoft.Win32;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace FortnitePorting.Views;

public partial class PluginUpdateView
{
    public PluginUpdateView()
    {
        InitializeComponent();

        void AddInstallation(DirectoryInfo directory, string prefix = "")
        {
            if (!double.TryParse(directory.Name, out var numberVersion)) return;
            var addonsPath = Path.Combine(directory.FullName, "scripts", "addons");
            if (!Directory.Exists(addonsPath)) return;
            Log.Information("Found Blender installation at {0}.", directory.FullName);
            var isSupported = numberVersion >= 3.0;
            var extraText = isSupported ? string.Empty : "(Unsupported)";

            if (!string.IsNullOrWhiteSpace(prefix)) prefix += " ";

            var toggleSwitch = new ToggleButton();
            toggleSwitch.Content = $"{prefix}Blender {directory.Name} {extraText}";
            toggleSwitch.IsEnabled = isSupported;
            toggleSwitch.Tag = directory;
            BlenderInstallationList.Items.Add(toggleSwitch);
        }

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

    private void OnClickFinished(object sender, RoutedEventArgs e)
    {
        var selectedVersions = BlenderInstallationList.Items
            .OfType<ToggleButton>()
            .Where(x => x.IsChecked.HasValue && x.IsChecked.Value)
            .Select(x => x.Tag as DirectoryInfo).ToArray();

        if (selectedVersions.Length == 0)
        {
            Close();
            return;
        }

        using var addonZip = new ZipArchive(new FileStream("FortnitePortingServer.zip", FileMode.Open));
        foreach (var selectedVersion in selectedVersions)
        {
            var addonPath = Path.Combine(selectedVersion.FullName, "scripts", "addons");
            addonZip.ExtractToDirectory(addonPath, overwriteFiles: true);
        }

        Close();
        MessageBox.Show($"Successfully updated plugin for Blender {selectedVersions.Select(x => x.Name).CommaJoin()}. Please remember to enable the plugin (if this is your first time installing) and restart Blender.", "Updated Plugin Successfully", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public static class SteamDetection
{

    public static List<AppInfo> GetSteamApps(string[] steamLibs)
    {
        var apps = new List<AppInfo>();
        foreach (var lib in steamLibs)
        {
            var appMetaDataPath = Path.Combine(lib, "SteamApps");
            var files = Directory.GetFiles(appMetaDataPath, "*.acf");
            apps.AddRange(files.Select(GetAppInfo).Where(appInfo => appInfo is not null));
        }
        return apps;
    }

    public static AppInfo? GetAppInfo(string appMetaFile)
    {
        var fileDataLines = File.ReadAllLines(appMetaFile);
        var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in fileDataLines)
        {
            var match = Regex.Match(line, @"\s*""(?<key>\w+)""\s+""(?<val>.*)""");
            if (!match.Success) continue;
            var key = match.Groups["key"].Value;
            var val = match.Groups["val"].Value;
            dic[key] = val;
        }

        AppInfo? appInfo;

        if (dic.Keys.Count <= 0) return null;

        appInfo = new AppInfo();
        var appId = dic["appid"];
        var name = dic["name"];
        var installDir = dic["installDir"];

        var path = Path.GetDirectoryName(appMetaFile);
        var libGameRoot = Path.Combine(path, "common", installDir);

        if (!Directory.Exists(libGameRoot)) return null;

        appInfo.Id = appId;
        appInfo.Name = name;
        appInfo.GameRoot = libGameRoot;

        return appInfo;
    }

    public static string[] GetSteamLibs()
    {
        var steamPath = GetSteamPath();
        if (steamPath is null) return Array.Empty<string>();
        var libraries = new List<string> { steamPath };

        var listFile = Path.Combine(steamPath, @"steamapps\libraryfolders.vdf");
        if (!File.Exists(listFile)) return Array.Empty<string>();
        var lines = File.ReadAllLines(listFile);
        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"""(?<path>\w:\\\\.*)""");
            if (!match.Success) continue;
            var path = match.Groups["path"].Value.Replace(@"\\", @"\");
            if (Directory.Exists(path))
            {
                libraries.Add(path);
            }
        }
        return libraries.ToArray();
    }

    private static string? GetSteamPath()
    {
        var bit64 = (string?) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", "");
        var bit32 = (string?) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", "");

        return bit64 ?? bit32 ?? null;
    }

    public class AppInfo
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string GameRoot { get; internal set; }
    }
}