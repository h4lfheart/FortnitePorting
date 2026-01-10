using System.Diagnostics;
using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Extensions;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Actors;

public class WorldActor : Actor
{
    public WorldActor(UWorld world, Transform? transform = null) : this(world.PersistentLevel.Load<ULevel>(), transform)
    {
    }
    
    public WorldActor(ULevel? level, Transform? transform = null) : base(level?.Name ?? "World")
    {
        Components.Add(new SpatialComponent("WorldRoot", transform));
        if (level is not null) 
            AddActors(level);
    }
    
    private void AddActors(ULevel level)
    {
        foreach (var actorPtr in level.Actors)
        {
            if (actorPtr is null || actorPtr.IsNull) continue;

            var actor = actorPtr.Load();
            if (actor is null) continue;
            if (actor.ExportType == "LODActor") continue;
            
            if (actor.TryGetValue(out FSoftObjectPath[] additionalWorlds, "AdditionalWorlds"))
            {
                foreach (var additionalWorldPath in additionalWorlds)
                {
                    if (!additionalWorldPath.TryLoad<UWorld>(out var subWorld)) continue;
                    if (actor.GetOrDefault<USceneComponent?>("RootComponent") is not { } sceneComponent) continue;
                    
                    var transform = new FTransform(
                        sceneComponent.GetRelativeRotation(),
                        sceneComponent.GetRelativeLocation(),
                        sceneComponent.GetRelativeScale3D());
                    
                    var subWorldActor = new WorldActor(subWorld, transform);
                    Children.Add(subWorldActor);
                }
            }
            else if (actor.TryGetValue(out UStaticMeshComponent component, "StaticMeshComponent"))
            {
                AddStaticMesh(actor, component, this);
            }
        }
    }
    
    private void AddStaticMesh(UObject levelActor, UStaticMeshComponent component, Actor parent)
    {
        if (!component.GetStaticMesh().TryLoad(out UStaticMesh staticMesh)) return;

        var transform = new FTransform(
            component.GetRelativeRotation(),
            component.GetRelativeLocation(),
            component.GetRelativeScale3D());
        
        levelActor.GatherTemplateProperties();
        
        var textureData = levelActor.GetAllProperties<UBuildingTextureData>("TextureData");
        
        var meshActor = new MeshActor(staticMesh, transform, textureData)
        {
            Name = levelActor.Name
        };

        parent.Children.Add(meshActor);

    }
}