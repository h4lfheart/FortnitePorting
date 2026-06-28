using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using FortnitePorting.Framework;

namespace FortnitePorting.Services;

public class WindowManagerService : IService
{
    private readonly List<Window> _openWindows = [];

    public void Register(Window window) => _openWindows.Add(window);
    public void Unregister(Window window) => _openWindows.Remove(window);

    public void CloseAllPreviews()
    {
        foreach (var window in _openWindows.OfType<IPreviewWindow>().ToArray())
            ((Window)window).Close();
    }
}
