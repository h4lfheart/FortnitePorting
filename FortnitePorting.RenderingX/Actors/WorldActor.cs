using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Actors;

public class WorldActor : Actor
{
    public TransformComponent Transform;

    public override void Initialize()
    {
        base.Initialize();
        
        Transform = AddComponent<TransformComponent>();
    }
}