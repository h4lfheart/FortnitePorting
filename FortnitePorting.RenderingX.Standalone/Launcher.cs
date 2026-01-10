using System.Diagnostics;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using FortnitePorting.RenderingX.Actors;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Managers;
using FortnitePorting.RenderingX.Renderers;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;

namespace FortnitePorting.RenderingX.Standalone;

public class Launcher
{
    private static DefaultFileProvider _provider;
    
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        if (args.Length != 3)
        {
            Log.Fatal("Invalid argument count, should be 3.");
        }
        
        InitializeUEParse(args[0], args[1], args[2]);
        RunViewer();
    }

    private static void RunViewer()
    {
        var scene = new Scene();
        
        var window = new RenderingXContext(scene);
        
        var sw = Stopwatch.StartNew();
        {
            var root = new Actor("Root");
            scene.AddActor(root);
            
            var cameraActor = new CameraActor("MainCamera");
            cameraActor.Camera.Transform.Position = new Vector3(5, 5, 5);
            cameraActor.Camera.LookAt(Vector3.Zero);
            root.Children.Add(cameraActor);

            scene.ActiveCamera = cameraActor.Camera;

            var grid = new Actor("Grid");
            grid.Components.Add(new GridMeshComponent());
            root.Children.Add(grid);

            var world = new WorldActor(_provider.LoadPackageObject<UWorld>(
                "FortniteGame/Content/Athena/Artemis/Maps/Buildings/3x3/Artemis_3x3_Generic_House_a"));
            root.Children.Add(world);
        }
        sw.Stop();
        Log.Information($"Finished in {sw.Elapsed.TotalSeconds:N3}");
        
        window.Run();
    }

    private static void InitializeUEParse(string archivePath, string mainKey, string mappingsPath)
    {
        OodleHelper.DownloadOodleDll();
        OodleHelper.Initialize(OodleHelper.OODLE_DLL_NAME);

        _provider = new DefaultFileProvider(archivePath, SearchOption.TopDirectoryOnly, new VersionContainer(EGame.GAME_UE5_8), StringComparer.OrdinalIgnoreCase);
        _provider.Initialize();
    
        _provider.SubmitKey(new FGuid(), new FAesKey(mainKey));
        _provider.PostMount();
        _provider.LoadVirtualPaths();

        _provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);

    }
    
}