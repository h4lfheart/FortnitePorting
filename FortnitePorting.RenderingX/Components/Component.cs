using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Components;

public abstract class Component : Renderable
{
    public Actor Owner = null!;
}