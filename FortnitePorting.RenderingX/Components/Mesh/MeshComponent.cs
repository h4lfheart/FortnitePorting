using System.Numerics;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Data.Buffers;
using FortnitePorting.RenderingX.Data.Programs;
using FortnitePorting.RenderingX.Renderers;
using Buffer = System.Buffer;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class MeshComponent : Component
{
    public MeshRenderer Renderer;

    public override void Initialize()
    {
        base.Initialize();
        
        Renderer.Initialize();
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        Renderer.Update(deltaTime);
    }

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);
        
        Renderer.Render(camera);
    }

    public override void Destroy()
    {
        base.Destroy();
        
        Renderer.Destroy();
    }
}