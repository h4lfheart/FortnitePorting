using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Exceptions;

namespace FortnitePorting.RenderingX.Core;

public class Scene
{
    public CameraComponent? ActiveCamera { get; private set; }
    
    internal readonly Actor Root;

    private List<Manager> _managers = [];

    public Scene()
    {
        Root = new Actor
        {
            Name = "Root",
            SceneRef = this
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
        if (ActiveCamera is null)
        {
            throw new RenderingException("There is no active camera set to render with.");
        }
        
        Root.Render(ActiveCamera);

        foreach (var manager in _managers)
        {
            manager.Render(ActiveCamera);
        }
    }
    
    public T RegisterManager<T>() where T : Manager, new()
    {
        var manager = new T
        {
            SceneRef = this
        };
        
        manager.Initialize();
        
        _managers.Add(manager);
        
        return manager;
    }
    
    public T? GetManager<T>() where T : Manager
    {
        return _managers.OfType<T>().FirstOrDefault();
    }

    public void SetActiveCamera(CameraComponent camera)
    {
        ActiveCamera = camera;
    }
}