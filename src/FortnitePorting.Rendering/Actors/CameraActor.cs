using FortnitePorting.Rendering.Components.Rendering;
using FortnitePorting.Rendering.Components;
using FortnitePorting.Rendering.Core;

namespace FortnitePorting.Rendering.Actors;

public class CameraActor : Actor
{
    public CameraComponent Camera { get; }

    public CameraActor(string name) : base(name)
    {
        Camera = new CameraComponent();
        Components.Add(Camera);
    }
}