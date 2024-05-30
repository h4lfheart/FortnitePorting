using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.OpenGL.Rendering.Levels;
using FortnitePorting.OpenGL.Rendering.Materials;
using FortnitePorting.OpenGL.Rendering.Meshes;
using FortnitePorting.OpenGL.Rendering.Viewport;

namespace FortnitePorting.OpenGL.Rendering;

public class RenderManager : IRenderable
{
    public static RenderManager Instance;
    
    public Skybox Skybox;
    public Grid Grid;
    public Shader ObjectShader;
    private readonly Dictionary<int, Materials.Material> MaterialCache = new();
    
    public readonly List<IRenderable> Objects = [];

    public RenderManager()
    {
        Instance = this;
    }
    
    public void Setup()
    {
        ObjectShader = new Shader("shader");
        
        Skybox = new Skybox();
        Skybox.Setup();

        Grid = new Grid();
        Grid.Setup();
    }

    public void Render(Camera camera)
    {
        Skybox.Render(camera);
        
        Objects.ForEach(obj => obj.Render(camera));
        
        Grid.Render(camera);
    }

    public void Add(UObject obj)
    {
        IRenderable renderable = obj switch
        {
            UStaticMesh staticMesh => new StaticMesh(staticMesh),
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
    
    public Materials.Material GetOrAddMaterial(UMaterialInterface materialInterface, TextureData? textureData = null)
    {
        var hash = materialInterface.GetPathName().GetHashCode();
        if (textureData is not null) hash += textureData.Hash;
        
        if (MaterialCache.TryGetValue(hash, out var foundMaterial))
        {
            return foundMaterial;
        }

        MaterialCache[hash] = new Materials.Material(materialInterface, textureData);
        return MaterialCache[hash];
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