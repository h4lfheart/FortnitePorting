using System;
using System.Windows;
using FortnitePorting.Views.Extensions;
using MercuryCommons.Utilities.Extensions;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Shaders;

public class Shader : IDisposable
{
    private readonly ProgramHandle Handle;

    public Shader(string shaderName)
    {
        Handle = GL.CreateProgram();

        var vertexShader = LoadShader($"{shaderName}.vert", ShaderType.VertexShader);
        GL.AttachShader(Handle, vertexShader);
        
        var fragShader = LoadShader($"{shaderName}.frag", ShaderType.FragmentShader);
        GL.AttachShader(Handle, fragShader);
        
        GL.LinkProgram(Handle);
        
        GL.DetachShader(Handle, vertexShader);
        GL.DeleteShader(vertexShader);
        
        GL.DetachShader(Handle, fragShader);
        GL.DeleteShader(fragShader);
    }
    
    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public int GetUniformLocation(string name)
    {
        return GL.GetUniformLocation(Handle, name);
    }
    
    public void SetMatrix4(string name, Matrix4 value)
    {
        GL.UniformMatrix4f(GetUniformLocation(name), true, value);
    }
    
    public void SetUniform(string name, int value)
    {
        GL.Uniform1f(GetUniformLocation(name), value);
    }

    private ShaderHandle LoadShader(string name, ShaderType type)
    {
        var resourceStream = Application.GetResourceStream(new Uri($"/FortnitePorting;component/Resources/Shaders/{name}", UriKind.Relative));
        var content = resourceStream?.Stream.ReadToEnd().AsString();
        
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, content);
        GL.CompileShader(shader);
        
        GL.GetShaderInfoLog(shader, out var shaderInfo);
        if (!string.IsNullOrWhiteSpace(shaderInfo))
        {
            throw new Exception($"Error Compiling {type} {name}: {shaderInfo}");
        }

        return shader;
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
    }
}