using System.Collections.ObjectModel;

namespace FortnitePorting.RenderingX.Core;

public class Component(string name) : Renderable
{
    public string Name = name;
    
    public Actor Actor;
}