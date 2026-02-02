using System;
using System.ComponentModel;
using System.Text;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FFMpegCore.Enums;
using FortnitePorting.Extensions;
using Serilog.Events;

namespace FortnitePorting.Models.Information;

public partial class FPLogEvent : ObservableObject
{
    [ObservableProperty] private string _message;
    [ObservableProperty] private DateTimeOffset _timestamp;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TextColor), nameof(LevelText))] private ELogEventType _level;

    public string LevelText => Level.Description.ToUpper();
    
    public SolidColorBrush TextColor => Level switch
    {
        ELogEventType.Info => InformationBrush,
        ELogEventType.Warn => WarningBrush,
        ELogEventType.Error => ErrorBrush,
        ELogEventType.Fatal => FatalBrush,
    };

    private static readonly SolidColorBrush InformationBrush = SolidColorBrush.Parse("#E4E4E4");
    private static readonly SolidColorBrush WarningBrush = SolidColorBrush.Parse("#E4E421");
    private static readonly SolidColorBrush ErrorBrush = SolidColorBrush.Parse("#E42121");
    private static readonly SolidColorBrush FatalBrush = new(Colors.DarkRed);

    public FPLogEvent(LogEvent logEvent)
    {
        Message = logEvent.RenderMessage();
        Timestamp = logEvent.Timestamp;
        Level = logEvent.Level switch
        {
            LogEventLevel.Information => ELogEventType.Info,
            LogEventLevel.Warning => ELogEventType.Warn,
            LogEventLevel.Error => ELogEventType.Error,
            LogEventLevel.Fatal => ELogEventType.Fatal,
            _ => ELogEventType.None
        };

        var messageBuilder = new StringBuilder();
        messageBuilder.Append(logEvent.RenderMessage());
        if (logEvent.Exception is { } exception)
        {
            messageBuilder.Append('\n');
            messageBuilder.Append(exception);
        }

        Message = messageBuilder.ToString();
    }
}

public enum ELogEventType
{
    [Description("None")]
    None,
    
    [Description("Info")]
    Info,
    
    [Description("Warn")]
    Warn,
    
    [Description("Error")]
    Error,
    
    [Description("Fatal")]
    Fatal
}