using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using FortnitePorting.ViewModels;
using Ookii.Dialogs.Wpf;

namespace FortnitePorting.AppUtils;

public static class AppHelper
{
    public static void OpenWindow<T>() where T : Window, new()
    {
        var window = IsWindowOpen<T>() ? GetWindow<T>() : new T();
        window.Show();
        window.Focus();
    }

    public static void CloseWindow<T>() where T : Window, new()
    {
        if (!IsWindowOpen<T>()) return;

        var window = GetWindow<T>();
        window.Close();
    }

    private static bool IsWindowOpen<T>() where T : Window
    {
        return Application.Current.Windows.OfType<T>().Any();
    }

    private static T GetWindow<T>() where T : Window
    {
        return Application.Current.Windows.OfType<T>().First();
    }

    public static void Launch(string location, bool shellExecute = true)
    {
        Process.Start(new ProcessStartInfo { FileName = location, UseShellExecute = shellExecute });
    }

    public static bool TrySelectFolder(out string selectedPath)
    {
        var fileExplorer = new VistaFolderBrowserDialog { ShowNewFolderButton = true };

        if (fileExplorer.ShowDialog() == true)
        {
            selectedPath = fileExplorer.SelectedPath;
            return true;
        }

        selectedPath = string.Empty;
        return false;
    }

    public static bool Filter(string input, string filter)
    {
        var filters = filter.Trim().Split(' ');
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}