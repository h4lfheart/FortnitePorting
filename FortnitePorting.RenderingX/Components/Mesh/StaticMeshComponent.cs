using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class StaticMeshComponent(UStaticMesh mesh, List<KeyValuePair<UBuildingTextureData, int>>? textureData = null) : MeshComponent(new StaticMeshRenderer(mesh, textureData));