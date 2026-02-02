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
public partial class ModelPreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    
    [ObservableProperty] private RenderingXControl? _control;
    [ObservableProperty] private bool _isLoading;
    
    [ObservableProperty] private int _loadCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _loadName;
    [ObservableProperty] private bool _isLoadingWorld;
    
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

            var placementOffset = Vector3.Zero;
            foreach (var obj in objects)
            {
                switch (obj)
                {
                    case UStaticMesh staticMesh:
                    {
                        var actor = new MeshActor(staticMesh, new Transform
                        {
                            Position = placementOffset
                        });

                        var boundingBoxSize = actor.MeshComponent.Renderer.BoundingBox.GetSize();
                        placementOffset.X += boundingBoxSize.X * 0.01f + 1;
                        
                        PreviewRoot.Children.Add(actor);
                        break;
                    }
                    case USkeletalMesh skeletalMesh:
                    {
                        var actor = new MeshActor(skeletalMesh, new Transform
                        {
                            Position = placementOffset
                        });

                        var boundingBoxSize = actor.MeshComponent.Renderer.BoundingBox.GetSize();
                        placementOffset.X += boundingBoxSize.X * 0.01f + 1;
                        
                        PreviewRoot.Children.Add(actor);
                        break;
                    }
                    case ULevel level:
                    {
                        IsLoadingWorld = true;
                        
                        var worldActor = new WorldActor(level, progressHandler: progress =>
                        {
                            LoadCount = progress.Current;
                            TotalCount = progress.Total;
                            LoadName = progress.Name;
                        });
                        PreviewRoot.Children.Add(worldActor);
                        
                        IsLoadingWorld = false;
                        break;
                    }
                }
            }

            var meshSystem = Scene.ActorManager.GetSystem<MeshRenderSystem>();
            Scene.ActiveCamera?.FrameBounds(meshSystem?.GetBounds() ?? new FBox());

            IsLoading = false;
        });
    }
}