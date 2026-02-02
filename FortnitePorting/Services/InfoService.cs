using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Supabase.Postgrest.Exceptions;
using MessageData = FortnitePorting.Models.Information.MessageData;

namespace FortnitePorting.Services;

public partial class InfoService : ObservableObject, IService
{
    [ObservableProperty] private ObservableCollection<MessageData> _messages = [];
    [ObservableProperty] private DialogQueue _dialogQueue = new();
    [ObservableProperty] private BroadcastQueue _broadcastQueue = new();
    
    private readonly object _messageLock = new();
    
    public string LogFilePath;
    
    public DirectoryInfo LogsFolder => new(Path.Combine(App.ApplicationDataFolder.FullName, "Logs"));
    
    public InfoService()
    {
        TaskService.Exception += HandleException;
        
        Dispatcher.UIThread.UnhandledException += (sender, args) =>
        {
            args.Handled = true;
            HandleException(args.Exception);
        };
    }

    public void CreateLogger()
    {
        LogsFolder.Create();
        
        LogFilePath = Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(LogFilePath)
            .CreateLogger();
    }

    public void Message(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 3f, bool useButton = false, string buttonTitle = "", Action? buttonCommand = null)
    {
        Message(new MessageData(title, message, severity, autoClose, id, closeTime, useButton, buttonTitle, buttonCommand));
    }

    public void Message(MessageData data)
    {
        //if (!string.IsNullOrEmpty(data.Id))
           // Messages.RemoveAll(bar => bar.Id.Equals(data.Id));
        
        Messages.Add(data);
        if (!data.AutoClose) return;
        
        TaskService.Run(async () =>
        {
            await Task.Delay((int) (data.CloseTime * 1000));
            
            lock (_messageLock)
                Messages.Remove(data);
        });
    }
    
    public void UpdateMessage(string id, string message)
    {
        var foundInfoBar = Messages.FirstOrDefault(infoBar => infoBar.Id == id);

        foundInfoBar?.Message = message;
    }
    
    public void UpdateTitle(string id, string title)
    {
        var foundInfoBar = Messages.FirstOrDefault(infoBar => infoBar.Id == id);

        foundInfoBar?.Title = title;
    }
    
    public void CloseMessage(string id)
    {
        lock (_messageLock)
            Messages.RemoveAll(info => info.Id == id);
    }
    
    public void Dialog(string title, string? message = null, object? content = null, DialogButton[]? buttons = null, bool canClose = true)
    {
        DialogQueue.Enqueue(new DialogData
        {
            Title = title,
            Message = message,
            Content = content,
            Buttons = buttons is not null ? [..buttons] : [],
            CanClose = canClose
        });
    }
    
    public void Broadcast(BroadcastResponse broadcastResponse)
    {
        BroadcastQueue.Enqueue(new BroadcastData
        {
            Title = broadcastResponse.Title,
            Description = broadcastResponse.Description,
            Timestamp = broadcastResponse.Timestamp.ToLocalTime()
        });
    }
    
    public void HandleException(Exception e)
    {
        var exceptionString = e.ToString();
        Log.Error(exceptionString);

#if RELEASE
        if (SupaBase.IsLoggedIn)
        {
            TaskService.Run(async () => await Api.FortnitePorting.PostError(e));
        }
#endif
        
        Dialog("An unhandled exception has occurred", exceptionString, buttons: [
            new DialogButton
            {
                Text = "Open Logs Folder",
                Action = () => App.LaunchSelected(LogFilePath)
            }
        ]);
    }
}