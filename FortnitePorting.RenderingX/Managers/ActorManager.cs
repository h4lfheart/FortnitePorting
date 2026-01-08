using System.Collections.Specialized;
using FortnitePorting.RenderingX.Actors;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Exceptions;
using FortnitePorting.RenderingX.Systems;

namespace FortnitePorting.RenderingX.Managers;

public class ActorManager : Manager
{
    public Actor? RootActor
    {
        get;
        set
        {
            field = value;
            AddActor(value);
        }
    }
    
    private readonly HashSet<Guid> _actors = [];

    private readonly HashSet<ISystem> _systems = [];

    public ActorManager()
    {
        AddSystem(new MeshRenderSystem());
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        foreach (var system in _systems)
        {
            system.Update(deltaTime);
        }
    }

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);
        
        foreach (var system in _systems)
        {
            system.Render(camera);
        }
    }

    private void AddSystem<T>(T system) where T : ISystem
    {
        if (_systems.Any(s => s.GetType() == typeof(T)))
            throw new RenderingXException("System has already been registered with this actor manager");

        _systems.Add(system);
    }
 
    private void AddActor(Actor? actor)
    {
        if (actor is null)
            return;
        
        if (!_actors.Add(actor.Guid))
            throw new RenderingXException($"{actor.Name} has already been added to the actor manager");
        
        if (actor.Manager is not null)
            throw new RenderingXException($"{actor.Name} has already been registered with another actor manager");

        actor.Manager = this;

        foreach (var component in actor.Components)
        {
            AddComponent(component);
        }
        
        foreach (var child in actor.Children)
        {
            AddActor(child);
        }
        
        actor.Children.CollectionChanged += ChildrenOnCollectionChanged;
        actor.Components.CollectionChanged += ComponentsOnCollectionChanged;
    }
    
    private void RemoveActor(Actor? actor)
    {
        if (actor is null)
            return;
        
        if (!_actors.Remove(actor.Guid) || actor.Manager != this)
            throw new RenderingXException($"{actor.Name} has is not a part of this actor manager");

        actor.Manager = null;

        foreach (var component in actor.Components)
        {
            RemoveComponent(component);
        }
        
        foreach (var child in actor.Children)
        {
            RemoveActor(child);
        }
        
        actor.Children.CollectionChanged -= ChildrenOnCollectionChanged;
        actor.Components.CollectionChanged -= ComponentsOnCollectionChanged;
    }
    
    private void AddComponent(Component? component)
    {
        if (component is null) return;

        foreach (var system in _systems.Where(system => system.Supports(component.GetType())))
        {
            system.RegisterComponent(component);
        }
    }
    
    private void RemoveComponent(Component? component)
    {
        if (component is null) return;

        foreach (var system in _systems.Where(system => system.Supports(component.GetType())))
        {
            system.UnregisterComponent(component);
        }
    }
    

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var actor in e.NewItems!.Cast<Actor>())
                {
                    AddActor(actor);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var actor in e.OldItems!.Cast<Actor>())
                {
                    RemoveActor(actor);
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