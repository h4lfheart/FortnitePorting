using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;

namespace FortnitePorting.RenderingX.Core;

public class Actor(string name = "Actor", Guid? guid = null) : Renderable
{
    public string Name = name;
    public Guid Guid = guid ?? Guid.NewGuid();
    
    public Actor? Parent = null;
    public Scene Scene;

    public TransformComponent? Transform => GetComponent<TransformComponent>();
    
    private readonly List<Actor> _children = [];
    private readonly List<Component> _components = [];

    public Actor AddChild(Actor actor)
    {
        _children.Add(actor);
        actor.SetParent(this);
        actor.Scene = Scene;
        return actor;
    }

    public void SetParent(Actor parent)
    {
        Parent = parent;
    }
    
    public T AddComponent<T>() where T : Component, new()
    {
        var component = new T();
        component.Owner = this;
        _components.Add(component);
        component.Initialize();
        return component;
    }

    public T AddComponent<T>(T component) where T : Component
    {
        component.Owner = this;
        _components.Add(component);
        component.Initialize();
        return component;
    }

    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    public T[] GetComponents<T>() where T : Component
    {
        return _components.OfType<T>().ToArray();
    }

    public bool HasComponent<T>() where T : Component
    {
        return _components.OfType<T>().Any();
    }

    public void RemoveComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component is null) return;
        
        component.Destroy();
        _components.Remove(component);
    }

    public void RemoveComponent(Component component)
    {
        if (_components.Contains(component))
        {
            component.Destroy();
            _components.Remove(component);
        }
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        foreach (var component in _components)
        {
            component.Update(deltaTime);
        }
        
        foreach (var child in _children)
        {
            child.Update(deltaTime);
        }
    }

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);
        
        foreach (var component in _components)
        {
            component.Render(camera);
        }
        
        foreach (var child in _children)
        {
            child.Render(camera);
        }
    }
}