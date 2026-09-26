using FortnitePorting.Rendering.Components.Rendering;
using FortnitePorting.Rendering.Components;

namespace FortnitePorting.Rendering.Core;

public class Renderable
{
    public virtual void Initialize() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Render(CameraComponent camera) { }
    public virtual void Destroy() { }
}