using System.Collections.Specialized;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Exceptions;

namespace FortnitePorting.RenderingX.Managers;

public class ActorManager : Manager
{
    private readonly HashSet<Guid> _actors = [];

    public Actor? RootActor
    {
        get;
        set
        {
            field = value;
            AddActor(value);
        }
    }

    private void AddActor(Actor? actor)
    {
        if (actor is null)
            return;
        
        if (!_actors.Add(actor.Guid))
            throw new SceneException($"{actor.Name} has already been added to the actor manager");
        
        if (actor.Manager is not null)
            throw new SceneException($"{actor.Name} has already been registered with another actor manager");

        actor.Manager = this;

        foreach (var component in actor.Components)
        {
            // TODO add component handling (i.e. mesh rendering system)
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
            throw new SceneException($"{actor.Name} has is not a part of this actor manager");

        actor.Manager = null;

        foreach (var component in actor.Components)
        {
            // TODO add component handling (i.e. mesh rendering system)
        }
        
        foreach (var child in actor.Children)
        {
            RemoveActor(child);
        }
        
        actor.Children.CollectionChanged -= ChildrenOnCollectionChanged;
        actor.Components.CollectionChanged -= ComponentsOnCollectionChanged;
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
        // TODO add component handling (i.e. mesh rendering system)
    }
}