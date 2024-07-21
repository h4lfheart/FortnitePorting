using System.Text;
using Avalonia.Platform;
using FortnitePorting.Shared.Extensions;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace FortnitePorting.Rendering.Rendering.Materials;

public class Shader : IDisposable
{
    private readonly int Handle;

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

    public void SetMatrix4(string name, Matrix4 value, bool transpose = true)
    {
        GL.UniformMatrix4f(GetUniformLocation(name), 1, transpose, value);
    }

    public void SetUniform(string name, int value)
    {
        GL.Uniform1i(GetUniformLocation(name), value);
    }

    public void SetUniform(string name, float value)
    {
        GL.Uniform1f(GetUniformLocation(name), value);
    }

    public void SetUniform3(string name, Vector3 pos)
    {
        GL.Uniform3f(GetUniformLocation(name), pos.X, pos.Y, pos.Z);
    }

    private int LoadShader(string name, ShaderType type)
    {
        var stream = AssetLoader.Open(new Uri($"avares://FortnitePorting.Rendering/Assets/Shaders/{name}"));
        var content = Encoding.UTF8.GetString(stream.ReadToEnd());

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