using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class MeshComponent : SpatialComponent
{
    private readonly MeshRenderer _renderer;

    public MeshComponent(MeshRenderer renderer)
    {
        _renderer = renderer;
        
        _renderer.Component = this;
        _renderer.Initialize();
    } 
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        _renderer.Update(deltaTime);
    }

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);
        
        _renderer.Render(camera);
    }

    public override void Destroy()
    {
        base.Destroy();
        
        _renderer.Destroy();
    }
}