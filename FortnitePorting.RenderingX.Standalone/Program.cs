using FortnitePorting.RenderingX;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
    
    
var window = new RenderingXWindow();
window.Run();