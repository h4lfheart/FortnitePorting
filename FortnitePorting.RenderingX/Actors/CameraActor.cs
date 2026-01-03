using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Actors;

public class CameraActor : WorldActor
{
    public CameraComponent Camera;

    public override void Initialize()
    {
        base.Initialize();
        
        Camera = AddComponent<CameraComponent>();
    }

    public void MakeActiveCamera()
    {
        SceneRef?.SetActiveCamera(Camera);
    }
}