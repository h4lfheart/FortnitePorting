using System;
using System.Collections.Generic;
using FortnitePorting.OpenGL.Renderable;

namespace FortnitePorting.OpenGL;

public class Renderer : IRenderable
{
    private readonly List<IRenderable> Dynamic = new();
    private readonly List<IRenderable> Static = new();
    
    //private readonly Dictionary<string, Material> MaterialCache = new();
    
    public void Setup()
    {
        foreach (var renderable in Dynamic)
        {
            renderable.Setup();
        }
        
        foreach (var renderable in Static)
        {
            renderable.Setup();
        }
    }

    public void Render(Camera camera)
    {
        foreach (var renderable in Dynamic)
        {
            renderable.Render(camera);
        }
        
        foreach (var renderable in Static)
        {
            renderable.Render(camera);
        }
    }
    
    public void AddDynamic(IRenderable renderable)
    {
        renderable.Setup();
        Dynamic.Add(renderable);
    }
    
    public void AddStatic(IRenderable renderable)
    {
        renderable.Setup();
        Static.Add(renderable);
    }
    
    public void Clear()
    {
        Dynamic.Clear();
        //MaterialCache.Clear();
    }

    public void Dispose()
    {
        
    }
}

public interface IRenderable : IDisposable
{
    public void Setup();
    public void Render(Camera camera);
}