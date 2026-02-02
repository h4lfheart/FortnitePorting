using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;

namespace FortnitePorting.RenderingX.Systems;

public interface ISystem
{
    public Type[] ComponentTypes { get; }
    
    public void Update(float deltaTime);
    public void Render(CameraComponent camera);
    
    public void RegisterComponent(Component component);
    public void UnregisterComponent(Component component);
}

public static class SystemExtensions
{
    extension(ISystem system)
    {
        public bool Supports(Type targetType)
        {
            return system.ComponentTypes.Any(targetType.IsAssignableTo);
        }
    }
}