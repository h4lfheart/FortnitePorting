using System.Diagnostics;
using Serilog;

namespace FortnitePorting.Updater;

public static class Updater
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"Logs/FortnitePortingUpdater-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log")
            .CreateLogger();

        if (args.Length != 2)
        {
            Log.Error($"Invalid argument count. Only {args.Length} args were found, should be 2.");
            return;
        }

        try
        {
            var updatedFile = args[0].Replace("\"", string.Empty);
            var applicationFile = args[1].Replace("\"", string.Empty);

            Log.Information($"Application File: {applicationFile} | Exists: {File.Exists(applicationFile)}");
            Log.Information($"Update File: {updatedFile} | Exists: {File.Exists(updatedFile)}");
            
            while (IsExecutableRunning(applicationFile)) { }

            File.Delete(applicationFile);
            File.Move(updatedFile, applicationFile, overwrite: true);
            Log.Information($"Moved {updatedFile} to {applicationFile}");

            Process.Start(applicationFile);
            Log.Information($"Started {applicationFile}");
        }
        catch (Exception e)
        {
            Log.Fatal(e.ToString());
        }
    }
    
    public static bool IsExecutableRunning(string path)
    {
        try
        {
            var processes = Process.GetProcessesByName("FortnitePorting");
            return processes.Any(process => process.MainModule is { } mainModule && mainModule.FileName.Equals(path.Replace("/", "\\")));
        }
        catch (Exception)
        {
            return false;
        }

    }
}