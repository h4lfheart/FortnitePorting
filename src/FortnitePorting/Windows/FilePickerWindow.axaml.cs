using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using FortnitePorting.Services;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class FilePickerWindow : WindowBase<FilePickerWindowModel>
{
    private readonly TaskCompletionSource<string[]> _returnTask;

    private FilePickerWindow(TaskCompletionSource<string[]> returnTask, string windowName, string? startPath = null) : base(initializeWindowModel: false)
    {
        InitializeComponent();
        
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
        
        WindowModel.WindowName = windowName;
        WindowModel.StartPath = startPath;
        
        TaskService.Run(WindowModel.Initialize);
            
        _returnTask = returnTask;
    }

    public static async Task<string[]?> OpenBrowserAsync(string windowName = "File Picker", string? startPath = null)
    {
        var taskSource = new TaskCompletionSource<string[]>();
        
        await TaskService.RunDispatcherAsync(() =>
        {
            var window = new FilePickerWindow(taskSource, windowName, startPath);
            window.Show();
        });
        
        return await taskSource.Task;
    }

    private void OnSelectPressed(object? sender, RoutedEventArgs e)
    {
        _returnTask.TrySetResult(WindowModel.Context.GetSelectedFilePaths());
        Close();
    }
    
    
    private void OnCancelPressed(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnFileItemDoubleTapped(TreeItem item)
    {
        if (item.Type == ENodeType.Folder) return;
        
        _returnTask.TrySetResult([item.FilePath]);
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _returnTask.TrySetResult([]);
    }
}