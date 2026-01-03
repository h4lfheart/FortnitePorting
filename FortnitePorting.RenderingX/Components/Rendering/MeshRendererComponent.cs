
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Rendering;

public class MeshRendererComponent(MeshRenderer _renderer) : Component
{
    public override void Initialize()
    {
        base.Initialize();

        _renderer.Owner = this;
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