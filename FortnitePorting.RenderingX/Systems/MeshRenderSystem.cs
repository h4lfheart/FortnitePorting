using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Exceptions;

namespace FortnitePorting.RenderingX.Systems;

public class MeshRenderSystem : ISystem
{
    public Type[] ComponentTypes { get; } =
    [
        typeof(MeshComponent)
    ];

    private HashSet<MeshComponent> _registeredComponents = [];
    
    public void Update(float deltaTime)
    {
        foreach (var component in _registeredComponents)
        {
            component.Renderer.Update(deltaTime);
        }
    }

    public void Render(CameraComponent camera)
    {
        foreach (var component in _registeredComponents)
        {
            component.Renderer.Render(camera);
        }
    }

    public void RegisterComponent(Component component)
    {
        if (component is not MeshComponent meshComponent) return;

        if (!_registeredComponents.Add(meshComponent))
        {
            throw new RenderingXException("Mesh component has already been registered with this mesh render system.");
        }
    }

    public void UnregisterComponent(Component component)
    {
        if (component is not MeshComponent meshComponent) return;

        if (!_registeredComponents.Remove(meshComponent))
        {
            throw new RenderingXException("Mesh component is not registered with this mesh render system.");
        }
    }
}