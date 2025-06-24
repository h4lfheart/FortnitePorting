using FortnitePorting.RenderingX.Components;

namespace FortnitePorting.RenderingX.Managers;

public class ComponentManager<T> : Manager where T : Component
{
    public override void Initialize()
    {
        base.Initialize();

        Component.Created += RegisterComponent;
        Component.Destroyed += UnregisterComponent;
    }

    public override void Destroy()
    {
        base.Destroy();
        
        Component.Created -= RegisterComponent;
        Component.Destroyed -= UnregisterComponent;
    }

    protected virtual void OnComponentCreated(T component)
    {
        
    }
    
    protected virtual void OnComponentDestroyed(T component)
    {
        
    }

    private void RegisterComponent(Component obj)
    {
        if (obj is T component)
        {
            OnComponentCreated(component);
        }
    }
    
    private void UnregisterComponent(Component obj)
    {
        if (obj is T component)
        {
            OnComponentDestroyed(component);
        }
    }
}