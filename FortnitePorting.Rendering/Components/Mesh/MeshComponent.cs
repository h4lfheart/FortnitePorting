using FortnitePorting.Rendering.Renderers;
using FortnitePorting.Rendering.Components.Rendering;

namespace FortnitePorting.Rendering.Components.Mesh;

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