using System.Collections.ObjectModel;
using System.Collections.Specialized;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Managers;

namespace FortnitePorting.RenderingX.Core;

public class Actor : Renderable
{
    public string Name;
    public Guid Guid = Guid.NewGuid();

    public Actor? Parent;
    public SpatialComponent? RootComponent { get; private set; }

    public ComponentCollection Components;
    public ActorCollection Children;

    public ActorManager Manager;

    public Actor(string name = "Actor")
    {
        Name = name;
        Components = [];
        Children = [];
        
        Children.CollectionChanged += ChildrenOnCollectionChanged;
        Components.CollectionChanged += ComponentsOnCollectionChanged;
    }

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);

        // TODO render from actor manager
        foreach (var child in Children)
        {
            child.Render(camera);
        }
        
        // TODO render from component system (i.e. mesh render system picks up on mesh components and renders them)
        foreach (var component in Components)
        {
            component.Render(camera);
        }
    }

    public override void Destroy()
    {
        base.Destroy();
        
        foreach (var child in Children)
        {
            child.Destroy();
        }
        
        foreach (var component in Components)
        {
            component.Destroy();
        }
    }

    private void AddChild(Actor actor)
    {
        actor.Parent = this;
    }
    
    private void RemoveChild(Actor actor)
    {
        actor.Parent = null;
    }

    private void AddComponent(Component component)
    {
        component.Actor = this;

        if (RootComponent is null && component is SpatialComponent spatialComponent)
        {
            RootComponent = spatialComponent;
        }
    }

    private void RemoveComponent(Component component)
    {
        component.Actor = null;
    }

    
    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var actor in e.NewItems!.Cast<Actor>())
                {
                    AddChild(actor);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var actor in e.OldItems!.Cast<Actor>())
                {
                    RemoveChild(actor);
                }
                break;
        }
    }
    
    private void ComponentsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var component in e.NewItems!.Cast<Component>())
                {
                    AddComponent(component);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var component in e.OldItems!.Cast<Component>())
                {
                    RemoveComponent(component);
                }
                break;
        }
    }
}

public class ActorCollection : ObservableCollection<Actor>;
public class ComponentCollection : ObservableCollection<Component>;