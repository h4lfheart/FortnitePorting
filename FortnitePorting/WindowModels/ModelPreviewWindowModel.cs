using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Rendering;
using FortnitePorting.RenderingX;
using FortnitePorting.RenderingX.Actors;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Systems;
using FortnitePorting.Services;
using OpenTK.Mathematics;
using Serilog;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class ModelPreviewWindowModel : WindowModelBase
{
    [ObservableProperty] private RenderingXControl? _control;
    [ObservableProperty] private bool _isLoading;
    
    [ObservableProperty] private static RenderingXContext? _context;
    [ObservableProperty] private static Scene _scene = null!;
    [ObservableProperty] private static Actor _previewRoot = null!;

    public void InitializeContext()
    {
        if (Context is null)
        {
            Scene = new Scene();
            Context = new RenderingXContext(Scene);
        
            var root = new Actor("Root");
            Scene.AddActor(root);
            
            var cameraActor = new CameraActor("MainCamera");
            cameraActor.Camera.Transform.Position = new Vector3(5, 5, 5);
            cameraActor.Camera.LookAt(Vector3.Zero);
            root.Children.Add(cameraActor);

            Scene.ActiveCamera = cameraActor.Camera;

            var grid = new Actor("Grid");
            grid.Components.Add(new GridMeshComponent());
            root.Children.Add(grid);

            PreviewRoot = new Actor("PreviewRoot");
            root.Children.Add(PreviewRoot);
        }
        
        Control = new RenderingXControl(Context);
    }
    
    public void LoadScene(IEnumerable<UObject> objects)
    {
        Context?.EnqueueCommand(() =>
        {
            IsLoading = true;
            
            foreach (var existingChild in PreviewRoot.Children.ToArray())
            {
                PreviewRoot.Children.Remove(existingChild);
            }

            foreach (var obj in objects)
            {
                PreviewRoot.Children.Add(obj switch
                {
                    UStaticMesh staticMesh => new MeshActor(staticMesh),
                    USkeletalMesh skeletalMesh => new MeshActor(skeletalMesh),
                    ULevel level => new WorldActor(level),
                    _ => new Actor(obj.Name)
                });
            }

            var meshSystem = Scene.ActorManager.GetSystem<MeshRenderSystem>();
            Scene.ActiveCamera?.FrameBounds(meshSystem?.GetBounds() ?? new FBox());

            IsLoading = false;
        });
    }
}