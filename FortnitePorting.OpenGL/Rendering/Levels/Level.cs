using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.OpenGL.Rendering.Meshes;
using OpenTK.Mathematics;
using Serilog;

namespace FortnitePorting.OpenGL.Rendering.Levels;

public class Level : IRenderable
{
    public List<Actor> Actors = [];

    public Level(ULevel level)
    {
        ProcessLevel(level);
    }
    
    public void ProcessLevel(ULevel level)
    {
        var actorCount = 0;
        foreach (var actorLazy in level.Actors)
        {
            actorCount++;
            
            if (actorLazy is null || actorLazy.IsNull) continue;
            
            var actor = actorLazy.Load();
            if (actor is null) continue;
            if (actor.ExportType == "LODActor") continue;

            Log.Information("Processing Actor {0}: {1}/{2}", actor.Name, actorCount, level.Actors.Length);
            ProcessActor(actor);
        }
    }

    public void ProcessActor(UObject actorObject)
    {
        if (actorObject.TryGetValue(out UStaticMeshComponent staticMeshComponent, "StaticMeshComponent", "StaticMesh", "Mesh"))
        {
            var staticMesh = staticMeshComponent.GetStaticMesh().Load<UStaticMesh>();
            if (staticMesh is null) return;

            var location = staticMeshComponent.GetOrDefault("RelativeLocation", FVector.ZeroVector) * 0.01f;
            var rotation = staticMeshComponent.GetOrDefault("RelativeRotation", FRotator.ZeroRotator).Quaternion();
            var scale = staticMeshComponent.GetOrDefault("RelativeScale3D", FVector.OneVector);

            var mesh = new Mesh(staticMesh);
            var transform = Matrix4.CreateScale(scale.X, scale.Z, scale.Y) 
                * Matrix4.CreateFromQuaternion(new Quaternion(rotation.X, rotation.Z, rotation.Y, -rotation.W))
                * Matrix4.CreateTranslation(location.X, location.Z, location.Y);
            
            Actors.Add(new Actor(mesh, transform));
        }
    }
    
    public void Setup()
    {
        Actors.ForEach(actor => actor.Setup());
    }

    public void Render(Camera camera)
    {
        Actors.ForEach(actor => actor.Render(camera));
    }
    
    public void Dispose()
    {
        Actors.ForEach(actor => actor.Dispose());
    }
}