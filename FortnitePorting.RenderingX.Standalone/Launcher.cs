using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FortnitePorting.RenderingX.Actors;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Managers;
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
        window.Load += () =>
        {
            var actorManager = scene.RegisterManager<ActorManager>();
            var meshManager = scene.RegisterManager<MeshManager>();

            var camera = actorManager.CreateActor<CameraActor>("MainCamera");
            camera.MakeActiveCamera();
            camera.Transform.LocalPosition = new Vector3(5, 5, 5);
            camera.Camera.LookAt(Vector3.Zero);

            var grid = actorManager.CreateActor<GridActor>("Grid");

            var staticMesh = _provider.LoadPackageObject<UStaticMesh>("FortniteGame/Content/Environments/Apollo/Sets/Coliseum_Ruins/Props/Meshes/S_CR_PeelyStatue");
            
            var staticMeshActor = actorManager.CreateActor<StaticMeshActor>(staticMesh.Name);
            staticMeshActor.SetStaticMesh(staticMesh);
            
            var skeletalMesh = _provider.LoadPackageObject<USkeletalMesh>("FortniteGame/Content/Characters/Player/Male/Medium/Bodies/M_Med_Soldier_04/Meshes/SK_M_Med_Soldier_04");
           
            var skeletalMeshActor = actorManager.CreateActor<SkeletalMeshActor>(skeletalMesh.Name);
            skeletalMeshActor.Transform.LocalPosition = new Vector3(5, 0, 0);
            skeletalMeshActor.SetSkeletalMesh(skeletalMesh);
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