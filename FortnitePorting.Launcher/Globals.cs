global using static FortnitePorting.Launcher.Services.ApplicationService;
using Avalonia.Platform.Storage;

namespace FortnitePorting.Launcher;

public static class Globals
{
    public const string DEFAULT_FP_REPOSITORY = "https://fortniteporting.halfheart.dev/api/v3/repository";
    
    public static readonly FilePickerFileType ExecutableFileType = new("Executable") { Patterns = ["*.exe"] };
}