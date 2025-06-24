using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.Utils;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Managers;

public class InstancedMeshManager : ComponentManager<InstancedMeshComponent>
{
    private readonly Dictionary<string, InstancedMeshRenderer> _renderers = [];
    
    private readonly Dictionary<string, List<InstancedMeshComponent>> _renderQueue = [];

    // TODO dirty system so that transforms aren't recomputed every frame
    public override void Render(CameraComponent camera)
    {
        base.Render(camera);

        foreach (var (meshName, instancedMeshes) in _renderQueue)
        {
            if (instancedMeshes.Count == 0) continue;

            var renderer = GetOrCreateRenderer(meshName, instancedMeshes[0].Mesh);
            
            renderer.ClearTransforms();
            foreach (var instancedMeshComponent in instancedMeshes)
            {
                renderer.AddTransform(instancedMeshComponent.Owner.Transform!);
            }
            renderer.UpdateInstanceBuffer();
            
            renderer.Render(camera);
        }
    }

    protected override void OnComponentCreated(InstancedMeshComponent component)
    {
        base.OnComponentCreated(component);
        
        var meshName = component.Mesh.Name;

        var queue = _renderQueue.GetOrAdd(meshName, () => []);
        queue.Add(component);
    }

    protected override void OnComponentDestroyed(InstancedMeshComponent component)
    {
        base.OnComponentDestroyed(component);
        
        var meshName = component.Mesh.Name;
        
        var queue = _renderQueue.GetOrAdd(meshName, () => []);
        queue.Remove(component);
    }

    private InstancedMeshRenderer GetOrCreateRenderer(string meshName, UStaticMesh staticMesh)
    {
        return _renderers.GetOrAdd(meshName, () =>
        {
            staticMesh.TryConvert(out var convertedMesh);

            var renderer = new InstancedMeshRenderer(convertedMesh);
            renderer.Initialize();

            return renderer;
        });

    }

}