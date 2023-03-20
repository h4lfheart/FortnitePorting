using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.OpenGL.Renderable;
using FortnitePorting.OpenGL.Shaders;

namespace FortnitePorting.OpenGL;

public class Renderer : IRenderable
{
    public Skybox Skybox;
    public Shader MasterShader;
    public Grid Grid;
    
    private readonly List<IRenderable> Dynamic = new();
    private readonly List<IRenderable> Static = new();
    private readonly Dictionary<string, Material> MaterialCache = new();
    
    public void Setup()
    {
        Skybox = new Skybox();
        Skybox.Setup();
        
        MasterShader = new Shader("shader");
        MasterShader.Use();

        Grid = new Grid();
        Grid.Setup();
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
        
        Skybox.Render(camera);
        Grid.Render(camera);
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
        MaterialCache.Clear();
    }

    public Material? GetOrAddMaterial(UMaterialInterface? materialInterface)
    {
        if (materialInterface is null) return null;

        var path = materialInterface.GetPathName();
        if (MaterialCache.TryGetValue(path, out var foundMaterial))
        {
            return foundMaterial;
        }

        var material = new Material(materialInterface);
        MaterialCache[path] = new Material(materialInterface);
        return MaterialCache[path];
    }

    public void Dispose()
    {
        Skybox.Dispose();
        MasterShader.Dispose();
    }
}

public interface IRenderable : IDisposable
{
    public void Setup();
    public void Render(Camera camera);
}