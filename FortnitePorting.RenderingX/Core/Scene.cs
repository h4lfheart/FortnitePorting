using FortnitePorting.RenderingX.Actors;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Exceptions;
using FortnitePorting.RenderingX.Managers;

namespace FortnitePorting.RenderingX.Core;

public class Scene
{
    public CameraComponent? ActiveCamera { get; set; }

    public ActorManager ActorManager = new();
    
    public void Update(float deltaTime)
    {
        ActorManager.Update(deltaTime);
    }

    public void Render()
    {
        if (ActiveCamera is null)
        {
            throw new RenderingXException("There is no active camera set to render with.");
        }
        
        ActorManager.Render(ActiveCamera);
    }

    public void Destroy()
    {
        ActorManager.Destroy();
        
    }

    public void AddActor(Actor actor)
    {
        if (ActorManager.RootActor is null)
        {
            ActorManager.RootActor = actor;
        }
        else
        {
            ActorManager.RootActor.Children.Add(actor);
        }
    }
}