using FortnitePorting.RenderingX.Components.Rendering;

namespace FortnitePorting.RenderingX.Core;

public class Actor: Renderable
{
    public string Name = "Actor";
    public Guid Guid = Guid.NewGuid();

    public Actor? Parent;
    public Scene? SceneRef;

    private List<Component> _components = [];
    private List<Actor> _children = [];

    public void AddChild(Actor child)
    {
        child.Parent = this;
        child.SceneRef = SceneRef;
        _children.Add(child);
    }

    public void RemoveChild(Actor child)
    {
        child.Parent = null;
        child.SceneRef = null;
        _children.Remove(child);
    }

    public void AttachTo(Actor newParent)
    {
        Parent?.RemoveChild(this);
        newParent.AddChild(this);
    }

    public T AddComponent<T>() where T : Component, new()
    {
        return AddComponent(new T());
    }
    
    public T AddComponent<T>(T component) where T : Component
    {
        component.Owner = this;
        component.Initialize();
        
        _components.Add(component);
        return component;
    }
    
    public T? GetComponent<T>() where T : Component, new()
    {
        return _components.OfType<T>().FirstOrDefault();
    }
    
    public IEnumerable<T> GetComponents<T>() where T : Component, new()
    {
        return _components.OfType<T>();
    }

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);

        foreach (var child in _children)
        {
            child.Render(camera);
        }
        
        foreach (var component in _components)
        {
            component.Render(camera);
        }
    }
}