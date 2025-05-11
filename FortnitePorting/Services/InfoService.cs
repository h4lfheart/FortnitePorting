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
using FortnitePorting.Models.Serilog;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.App;
using FortnitePorting.Shared.Services;
using FortnitePorting.Views;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Supabase.Postgrest.Exceptions;
using TitleData = FortnitePorting.Models.Information.TitleData;

namespace FortnitePorting.Services;

public partial class InfoService : ObservableObject, ILogEventSink, IService
{
    [ObservableProperty] private ObservableCollection<FortnitePortingLogEvent> _logs = [];
    [ObservableProperty] private ObservableCollection<MessageData> _messages = [];
    [ObservableProperty] private TitleData? _titleData;
    
    public string LogFilePath;
    public readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    
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
            .WriteTo.Sink(this)
            .CreateLogger();
    }
    
    public void Message(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 3f, bool useButton = false, string buttonTitle = "", Action? buttonCommand = null)
    {
        Message(new MessageData(title, message, severity, autoClose, id, closeTime, useButton, buttonTitle, buttonCommand));
    }

    public void Message(MessageData data)
    {
        if (!string.IsNullOrEmpty(data.Id))
            Messages.RemoveAll(bar => bar.Id.Equals(data.Id));
        
        Messages.Add(data);
        if (!data.AutoClose) return;
        
        TaskService.Run(async () =>
        {
            await Task.Delay((int) (data.CloseTime * 1000));
            Messages.Remove(data);
        });
    }
    
    public void UpdateMessage(string id, string message)
    {
        var foundInfoBar = Messages.FirstOrDefault(infoBar => infoBar.Id == id);
        if (foundInfoBar is null) return;
        
        foundInfoBar.Message = message;
    }
    
    public void CloseMessage(string id)
    {
        Messages.RemoveAll(info => info.Id == id);
    }
    
    public void Dialog(string title, string content, string? primaryButtonText = null, Action? primaryButtonAction = null)
    {
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Continue",
                PrimaryButtonText = primaryButtonText,
                PrimaryButtonCommand = primaryButtonAction is not null ? new RelayCommand(primaryButtonAction) : null
            };
            
            await dialog.ShowAsync();
        });
    }
    
    public void Title(string title, string subTitle, float time = 5.0f)
    {
        TitleData = new TitleData(title, subTitle);
        TaskService.Run(async () =>
        {
            await Task.Delay((int) (time * 1000));
            TitleData = null;
        });
    }
    
    public void HandleException(Exception e)
    {
        var exceptionString = e.ToString();
        Log.Error(exceptionString);

        if (SupaBase.IsLoggedIn)
        {
            TaskService.Run(async () =>
            {
                try
                {
                    await SupaBase.Client.From<Error>().Insert(new Error
                    {
                        Version = Globals.Version.GetDisplayString(),
                        Message = $"{e.GetType().FullName}: {e.Message}",
                        StackTrace = e.StackTrace!.SubstringAfter("at ")
                    });
                }
                catch (PostgrestException)
                {
                    
                }
            });
        }
                
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "An unhandled exception has occurred",
                Content = exceptionString,
                
                PrimaryButtonText = "Open Log",
                PrimaryButtonCommand = new RelayCommand(() => App.LaunchSelected(LogFilePath)),
                SecondaryButtonText = "Open Console",
                SecondaryButtonCommand = new RelayCommand(() => Navigation.App.Open<ConsoleView>()),
                CloseButtonText = "Continue",
            };
            
            await dialog.ShowAsync();
        });
    }

    public void Emit(LogEvent logEvent)
    {
        Logs.Add(new FortnitePortingLogEvent(logEvent));
    }
}