using FortnitePorting.Rendering.Actors;
using FortnitePorting.Rendering.Core;

namespace FortnitePorting.Rendering.Components;

public class Component(string name)
{
    public string Name = name;
    
    public Actor? Actor;
}