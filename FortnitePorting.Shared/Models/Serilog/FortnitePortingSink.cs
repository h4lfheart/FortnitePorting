using System.Collections.ObjectModel;
using FortnitePorting.Shared.Extensions;
using Serilog.Core;
using Serilog.Events;

namespace FortnitePorting.Shared.Models.Serilog;

public class FortnitePortingSink : ILogEventSink
{
    public static ObservableCollection<FortnitePortingLogEvent> Logs = [];
    
    public void Emit(LogEvent logEvent)
    {
        Logs.Add(new FortnitePortingLogEvent(logEvent));
    }
}