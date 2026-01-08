using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Exceptions;
using FortnitePorting.RenderingX.Renderers;
using Serilog;

namespace FortnitePorting.RenderingX.Systems;

public class InstancedMeshRenderSystem : ISystem
{
    public Type[] ComponentTypes { get; } =
    [
        typeof(InstancedMeshComponent)
    ];

    private HashSet<InstancedMeshComponent> _registeredComponents = [];
    
    private Dictionary<string, InstancedMeshRenderer> _renderers = [];
    
    public void Update(float deltaTime)
    {
        
    }

    public void Render(CameraComponent camera)
    {
        foreach (var renderer in _renderers)
        {
            renderer.Value.ClearTransforms();
        }

        foreach (var component in _registeredComponents)
        {
            var renderer = GetOrCreateRenderer(component.Mesh.Name, component.Mesh);
            renderer.AddTransform(component);
        }
        
        foreach (var renderer in _renderers.Values)
        {
            renderer.UpdateInstanceBuffer();
            renderer.Render(camera);
        }
    }
    
    private InstancedMeshRenderer GetOrCreateRenderer(string meshName, UStaticMesh staticMesh)
    {
        if (_renderers.TryGetValue(meshName, out var renderer)) 
            return renderer;
        
        renderer = new InstancedMeshRenderer(staticMesh, lodLevel: 0);
        renderer.Initialize();
        _renderers[meshName] = renderer;

        return renderer;
    }

    public void RegisterComponent(Component component)
    {
        if (component is not InstancedMeshComponent meshComponent) return;

        if (!_registeredComponents.Add(meshComponent))
        {
            throw new RenderingXException("Instanced mesh component has already been registered with this mesh render system.");
        }
        
        var meshName = meshComponent.Mesh.Name;
        
        var renderer = GetOrCreateRenderer(meshName, meshComponent.Mesh);
        renderer.AddTransform(meshComponent);
        renderer.UpdateInstanceBuffer();
    }

    public void UnregisterComponent(Component component)
    {
        if (component is not InstancedMeshComponent meshComponent) return;

        if (!_registeredComponents.Remove(meshComponent))
        {
            throw new RenderingXException("Instanced mesh component is not registered with this mesh render system.");
        }
    }
}