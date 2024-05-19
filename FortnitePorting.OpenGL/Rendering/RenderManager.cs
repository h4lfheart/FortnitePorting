using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.OpenGL.Materials;
using FortnitePorting.OpenGL.Rendering.Levels;
using FortnitePorting.OpenGL.Rendering.Viewport;
using Mesh = FortnitePorting.OpenGL.Rendering.Meshes.Mesh;

namespace FortnitePorting.OpenGL.Rendering;

public class RenderManager : IRenderable
{
    public static RenderManager? Instance;
    
    public Skybox Skybox;
    public Grid Grid;
    public Shader ObjectShader;
    private readonly Dictionary<string, Materials.Material> MaterialCache = new();
    
    public readonly List<IRenderable> Objects = [];

    public RenderManager()
    {
        Instance = this;
    }
    
    public void Setup()
    {
        ObjectShader = new Shader("shader");
        ObjectShader.Use();
        
        Skybox = new Skybox();
        Skybox.Setup();

        Grid = new Grid();
        Grid.Setup();
    }

    public void Render(Camera camera)
    {
        Skybox.Render(camera);
        Grid.Render(camera);
        
        Objects.ForEach(obj => obj.Render(camera));
        
    }

    public void Add(UObject obj)
    {
        IRenderable renderable = obj switch
        {
            UStaticMesh staticMesh => new Mesh(staticMesh),
            USkeletalMesh skeletalMesh => new Mesh(skeletalMesh),
            ULevel level => new Level(level)
        };
        
        renderable.Setup();
        Objects.Add(renderable);
    }

    public void Clear()
    {
        Objects.Clear();
        MaterialCache.Clear();
    }
    
    public Materials.Material? GetOrAddMaterial(UMaterialInterface? materialInterface, TextureData? textureData = null)
    {
        if (materialInterface is null) return null;

        var path = materialInterface.GetPathName();
        if (MaterialCache.TryGetValue(path, out var foundMaterial))
        {
            return foundMaterial;
        }

        MaterialCache[path] = new Materials.Material(materialInterface, textureData);
        return MaterialCache[path];
    }
    
    public void Dispose()
    {
        Clear();
        
        Objects.ForEach(obj => obj.Dispose());
        ObjectShader.Dispose();
        Skybox.Dispose();
        Grid.Dispose();
    }
}