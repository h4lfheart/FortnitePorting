using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;

namespace FortnitePorting.RenderingX.Core;

public class Renderable
{
    public virtual void Initialize() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Render(CameraComponent camera) { }
    public virtual void Destroy() { }
}