using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class MeshComponent : SpatialComponent
{
    public readonly MeshRenderer Renderer;

    public MeshComponent(MeshRenderer renderer)
    {
        Renderer = renderer;
        
        Renderer.Component = this;
        Renderer.Initialize();
    }
}