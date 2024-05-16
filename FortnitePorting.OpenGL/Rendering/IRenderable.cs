namespace FortnitePorting.OpenGL.Rendering;

public interface IRenderable : IDisposable
{
    public void Setup();
    public void Render(Camera camera);
}