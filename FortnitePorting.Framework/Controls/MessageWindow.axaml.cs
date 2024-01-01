using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework.Application;
using FortnitePorting.Framework.Services;
using FortnitePorting.Framework.ViewModels;

namespace FortnitePorting.Framework.Controls;

public partial class MessageWindow : Window
{
    public static MessageWindow? ActiveWindow;
   
    public MessageWindow(MessageWindowModel model)
    {
        InitializeComponent();
        DataContext = model;
        ActiveWindow ??= this;
    }

    public static void Show(string title, string text, Window? owner = null, List<MessageWindowButton>? buttons = null)
    {
        var model = new MessageWindowModel
        {
            Title = title,
            Text = text
        };
        if (buttons is not null) model.Buttons = buttons;

        Show(model);
    }
    
    public static void Show(MessageWindowModel model)
    {
        if (ActiveWindow is not null) return;
        TaskService.RunDispatcher(() =>
        {
            new MessageWindow(model).Show();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        ActiveWindow = null;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        ActiveWindow = null;
        Close();
    }
}

public class MessageWindowModel
{
    public string Title { get; set; }
    public string Text { get; set; }
    public List<MessageWindowButton> Buttons { get; set; } = [MessageWindowButtons.Continue];
    public bool UseFallbackBackground => Environment.OSVersion.Platform != PlatformID.Win32NT || (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build < 22000);

    public ThemedViewModelBase Theme => AppBase.ThemeVM;
}

public class MessageWindowButton
{
    public string Label { get; set; }
    public Action<MessageWindow> OnClicked { get; set; }
    
    public MessageWindowButton(string label, Action<MessageWindow> onClicked)
    {
        Label = label;
        OnClicked = onClicked;
    }

    public void Command() => OnClicked.Invoke(MessageWindow.ActiveWindow);
}

public static class MessageWindowButtons
{
    public static readonly MessageWindowButton Continue = new("Continue", window => window.Close());
}