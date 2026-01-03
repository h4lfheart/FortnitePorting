using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Actors;

public class GridActor : Actor
{
    public override void Initialize()
    {
        base.Initialize();

        AddComponent(new MeshRendererComponent(new GridRenderer()));
    }
}