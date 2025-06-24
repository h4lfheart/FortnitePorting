using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Managers;

namespace FortnitePorting.RenderingX.Core;

public class Scene
{
    public Actor Root;

    public List<CameraComponent> Cameras = [];
    public CameraComponent ActiveCamera;

    private List<Manager> _managers = [];

    public Scene()
    {
        Root = new Actor("Root")
        {
            Scene = this
        };
        
        
    }

    public void Update(float deltaTime)
    {
        Root.Update(deltaTime);
        foreach (var manager in _managers)
        {
            manager.Update(deltaTime);
        }
    }

    public void Render()
    {
        foreach (var manager in _managers)
        {
            manager.Render(ActiveCamera);
        }
        
        Root.Render(ActiveCamera);
    }

    public Actor AddActor()
    {
        var newActor = new Actor();
        AddActor(newActor);
        return newActor;
    }
    
    public Actor AddActor(Actor actor)
    {
        Root.AddChild(actor);
        return actor;
    }

    public T AddManager<T>() where T : Manager, new()
    {
        var manager = new T();
        _managers.Add(manager);
        
        manager.Initialize();

        return manager;
    }
    
    public T GetManager<T>() where T : Manager, new()
    {
        return _managers.OfType<T>().FirstOrDefault();
    }
}