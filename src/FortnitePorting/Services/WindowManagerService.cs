using System;
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

    public TWindow? FindOpen<TWindow>() where TWindow : Window
        => _openWindows.OfType<TWindow>().FirstOrDefault();

    public TWindow GetOrShowPreview<TWindow>(Func<TWindow>? factory = null) where TWindow : Window
    {
        var existing = FindOpen<TWindow>();
        if (existing is not null)
        {
            existing.BringToTop();
            return existing;
        }

        var window = factory is not null ? factory() : Activator.CreateInstance<TWindow>();
        if (!window.IsVisible)
            window.Show();
        window.BringToTop();
        return window;
    }

    public void CloseAllPreviews()
    {
        foreach (var window in _openWindows.OfType<IPreviewWindow>().OfType<Window>().ToArray())
        {
            window.Close();
        }
    }
}
