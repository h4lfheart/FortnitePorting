using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.OpenGL.Rendering.Meshes;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Rendering.Levels;

public class Actor : IRenderable
{
    public StaticMesh Mesh;
    public Matrix4 Transform;

    public Actor(StaticMesh mesh, Matrix4 transform)
    {
        Mesh = mesh;
        Transform = transform;
    }
    
    public void Setup()
    {
        Mesh.Setup();
    }

    public void Render(Camera camera)
    {
        Mesh.Transform = Transform;
        Mesh.Render(camera);
    }
    
    public void Dispose()
    {
        Mesh.Dispose();
    }
}