using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Actors;

public class CameraActor : Actor
{
    public CameraComponent Camera { get; }

    public CameraActor(string name) : base(name)
    {
        Camera = new CameraComponent();
        Components.Add(Camera);
    }
}