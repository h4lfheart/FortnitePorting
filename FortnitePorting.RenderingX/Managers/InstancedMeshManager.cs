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
    private readonly Dictionary<string, InstancedMeshRenderer> _instancedMeshes = [];

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);

        foreach (var (key, renderer) in _instancedMeshes)
        {
            renderer.Render(camera);
        }
    }

    public void Add(UStaticMesh staticMesh, TransformComponent transform)
    {
        var renderer = _instancedMeshes.GetOrAdd(staticMesh.Name, () =>
        {
            staticMesh.TryConvert(out var convertedMesh);

            var renderer = new InstancedMeshRenderer(convertedMesh);
            renderer.Initialize();
            return renderer;
        });
        
        renderer.AddTransform(transform);
    }

}