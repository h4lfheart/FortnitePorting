using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace FortnitePorting.Viewer;

public class Program
{
    public static void Main()
    {
        var viewer =  new Viewer(GameWindowSettings.Default, new NativeWindowSettings
        {
            Size = new Vector2i(960, 540),
            NumberOfSamples = 8,
            WindowBorder = WindowBorder.Resizable,
            Profile = ContextProfile.Core,
            APIVersion = new Version(4, 6),
            Title = "Model Viewer",
            StartVisible = false,
            Flags = ContextFlags.ForwardCompatible
        });
        
        viewer.Run();
    }
}