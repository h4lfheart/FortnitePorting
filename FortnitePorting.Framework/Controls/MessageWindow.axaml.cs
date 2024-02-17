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
    public static bool IsWindowCreationQueued; // helps remove window creation overlap before avalonia init
    public static Queue<MessageWindowModel> WindowCreationQueue = [];
    
    public MessageWindow(MessageWindowModel model)
    {
        ActiveWindow = this;
        InitializeComponent();
        DataContext = model;
    }

    public static void Show(string title, string text, List<MessageWindowButton>? buttons = null)
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
        WindowCreationQueue.Enqueue(model);
        if (ActiveWindow is null && !IsWindowCreationQueued)
        {
            IsWindowCreationQueued = true;
            TaskService.RunDispatcher(() => new MessageWindow(WindowCreationQueue.Dequeue()).Show());
            IsWindowCreationQueued = false;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        ActiveWindow = null;
        if (WindowCreationQueue.Count > 0)
        {
            TaskService.RunDispatcher(() => new MessageWindow(WindowCreationQueue.Dequeue()).Show());
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
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