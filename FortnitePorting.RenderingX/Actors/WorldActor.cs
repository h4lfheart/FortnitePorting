using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Actors;

public class WorldActor : Actor
{
    public WorldActor(UWorld world, Transform? transform = null) : base(world.Name)
    {
        Components.Add(new SpatialComponent("WorldRoot", transform));
        AddActors(world);
    }
    
    private void AddActors(UWorld world)
    {
        var level = world.PersistentLevel.Load<ULevel>();
        if (level is null) return;
        
        var actors = world.PersistentLevel.Load<ULevel>()?.Actors ?? [];
        foreach (var actorPtr in actors)
        {
            if (actorPtr is null || actorPtr.IsNull) continue;

            var actor = actorPtr.Load<AActor>();
            if (actor is null) continue;
            if (actor.ExportType == "LODActor") continue;
            
            if (actor.TryGetValue(out UStaticMeshComponent component, "StaticMeshComponent"))
            {
                AddStaticMesh(actor, component, this);
            }
        }
    }
    
    private void AddStaticMesh(AActor levelActor, UStaticMeshComponent component, Actor parent)
    {
        var actor = new Actor(levelActor.Name);
        parent.Children.Add(actor);

        if (component.GetStaticMesh().TryLoad(out UStaticMesh staticMesh))
        {
            var transform = new FTransform(
                component.GetRelativeRotation(),
                component.GetRelativeLocation(),
                component.GetRelativeScale3D());
            
            var meshRenderer = new MeshComponent(new StaticMeshRenderer(staticMesh))
            {
                Transform =
                {
                    Position = new Vector3(transform.Translation.X, transform.Translation.Z, transform.Translation.Y) * 0.01f,
                    Rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Z, transform.Rotation.Y, -transform.Rotation.W),
                    Scale = new Vector3(transform.Scale3D.X, transform.Scale3D.Z, transform.Scale3D.Y)
                }
            };
            
            actor.Components.Add(meshRenderer);
        }
        
    }
}