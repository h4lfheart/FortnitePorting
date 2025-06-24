using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Components;

public abstract class Component : Renderable
{
    public Actor Owner = null!;
    
    public static event Action<Component>? Created;
    public static event Action<Component>? Destroyed;

    public override void Initialize()
    {
        base.Initialize();
        
        Created?.Invoke(this);
    }

    public override void Destroy()
    {
        base.Destroy();
        
        Destroyed?.Invoke(this);
    }
}