using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog.Events;
using Color = System.Drawing.Color;

namespace FortnitePorting.Shared.Models.Serilog;

public partial class FortnitePortingLogEvent : ObservableObject
{
    [ObservableProperty] private string _message;
    [ObservableProperty] private DateTimeOffset _timestamp;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TextColor))] private LogEventLevel _level;

    public string LogString => $"[{LogLevelString}] {Message}";

    public string LogLevelString => Level switch
    {
        LogEventLevel.Information => "INFO",
        LogEventLevel.Warning => "WARN",
        LogEventLevel.Error => "ERROR",
        LogEventLevel.Fatal => "FATAL"
    };
    
    public SolidColorBrush TextColor => Level switch
    {
        LogEventLevel.Information => InformationBrush,
        LogEventLevel.Warning => WarningBrush,
        LogEventLevel.Error => ErrorBrush,
        LogEventLevel.Fatal => FatalBrush,
    };

    private static readonly SolidColorBrush InformationBrush = SolidColorBrush.Parse("#E4E4E4");
    private static readonly SolidColorBrush WarningBrush = SolidColorBrush.Parse("#E4E421");
    private static readonly SolidColorBrush ErrorBrush = SolidColorBrush.Parse("#E42121");
    private static readonly SolidColorBrush FatalBrush = new(Colors.Black);

    public FortnitePortingLogEvent(LogEvent logEvent)
    {
        Message = logEvent.RenderMessage();
        Timestamp = logEvent.Timestamp;
        Level = logEvent.Level;
    }
}