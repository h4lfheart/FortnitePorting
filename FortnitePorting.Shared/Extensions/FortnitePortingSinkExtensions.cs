using FortnitePorting.Shared.Models.Serilog;
using Serilog;
using Serilog.Configuration;

namespace FortnitePorting.Shared.Extensions;

public static class FortnitePortingSinkExtensions
{
    public static LoggerConfiguration FortnitePorting(this LoggerSinkConfiguration loggerConfiguration)
    {
        return loggerConfiguration.Sink(new FortnitePortingSink());
    }
}