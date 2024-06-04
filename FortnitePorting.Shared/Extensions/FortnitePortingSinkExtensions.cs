using FortnitePorting.Shared.Models.Serilog;
using Serilog;
using Serilog.Configuration;

namespace FortnitePorting.Shared.Extensions;

public static class FortnitePortingSinkExtensions
{
    public static LoggerConfiguration FortnitePortingSink(this LoggerSinkConfiguration loggerConfiguration)
    {
        return loggerConfiguration.Sink(new FortnitePortingSink());
    }
}