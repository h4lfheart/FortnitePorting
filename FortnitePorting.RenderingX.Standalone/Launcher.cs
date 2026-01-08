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
        
        var window = new RenderingXWindow(scene);

        var root = new Actor("Root");
        scene.AddActor(root);
        
        window.Load += () =>
        {
            var cameraActor = new CameraActor("MainCamera");
            cameraActor.Camera.Transform.Position = new Vector3(5, 5, 5);
            cameraActor.Camera.LookAt(Vector3.Zero);
            root.Children.Add(cameraActor);

            scene.ActiveCamera = cameraActor.Camera;

            var grid = new Actor("Grid");
            grid.Components.Add(new GridMeshComponent());
            root.Children.Add(grid);
            
            var staticMesh = _provider.LoadPackageObject<UStaticMesh>("FortniteGame/Content/Environments/Apollo/Sets/Coliseum_Ruins/Props/Meshes/S_CR_PeelyStatue");

            var meshActor = new MeshActor(staticMesh, new Transform
            {
                Position = new Vector3(0, 0.06f, -0.5f),
                Rotation = Quaternion.FromEulerAngles(new Vector3(10f * MathF.PI / 180f, 0, 0)),
                Scale = new Vector3(0.2f)
            });
            root.Children.Add(meshActor);

            var world = new WorldActor(_provider.LoadPackageObject<UWorld>("FortniteGame/Content/Athena/Artemis/Maps/Buildings/1x1/Artemis_1x1_BusStation_a"), new Transform
            {
                Rotation = Quaternion.FromEulerAngles(new Vector3(0, MathF.PI, 0))
            });
            root.Children.Add(world);
            
        };
        
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