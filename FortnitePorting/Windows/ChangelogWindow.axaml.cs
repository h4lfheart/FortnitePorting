using System;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.AvaloniaEdit;
using FortnitePorting.Services;
using FortnitePorting.WindowModels;
using PropertiesContainer = FortnitePorting.Models.Viewers.PropertiesContainer;

namespace FortnitePorting.Windows;

public partial class ChangelogWindow : WindowBase<ChangelogWindowModel>
{
    public static ChangelogWindow? Instance;
    
    public ChangelogWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static void Preview(string? text)
    {
        text ??= "No Description.";
        
        if (Instance == null)
        {
            Instance = new ChangelogWindow();
            Instance.Show();
        }
        
        Instance.BringToTop();

        Instance.Editor.Document.Text = text;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        Instance = null;
    }
}