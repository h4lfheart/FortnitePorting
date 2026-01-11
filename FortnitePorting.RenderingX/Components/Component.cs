using FortnitePorting.RenderingX.Actors;
using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Components;

public class Component(string name)
{
    public string Name = name;
    
    public Actor? Actor;
}