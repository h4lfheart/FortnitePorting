using System.Reflection;
using System.Text;
using CUE4Parse.Utils;
using FortnitePorting.RenderingX.Exceptions;

namespace FortnitePorting.RenderingX.Data.Programs;

public class ShaderProgram : Program
{
    private readonly List<int> _shaderHandles = [];
    
    private readonly Dictionary<string, int> _uniformCache = [];
    
    public ShaderProgram(string shaderName)
    {
        _shaderHandles.Add(CompileShader(shaderName, ShaderType.FragmentShader));
        _shaderHandles.Add(CompileShader(shaderName, ShaderType.VertexShader));
    }

    public override void Link()
    {
        _shaderHandles.ForEach(shaderHandle => GL.AttachShader(_handle, shaderHandle));
        
        base.Link();
        
        _shaderHandles.ForEach(shaderHandle =>
        {
            GL.DetachShader(_handle, shaderHandle);
            GL.DeleteShader(shaderHandle);
        });
    }
    
    public void SetMatrix4(string name, Matrix4 value, bool transpose = true)
    {
        GL.UniformMatrix4f(GetUniformLocation(name), 1, transpose, ref value);
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
    
    private int GetUniformLocation(string name)
    {
        return _uniformCache.GetOrAdd(name, () => GL.GetUniformLocation(_handle, name));
    }


    private int CompileShader(string name, ShaderType type)
    {
        var ext = type switch
        {
            ShaderType.FragmentShader => "frag",
            ShaderType.VertexShader => "vert"
        };
        
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FortnitePorting.RenderingX.Shaders.{name}.{ext}")!;
        var streamBytes = new BinaryReader(stream).ReadBytes((int)stream.Length);
        var content = Encoding.UTF8.GetString(streamBytes);

        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, content);
        GL.CompileShader(shader);

        GL.GetShaderInfoLog(shader, out var shaderInfo);
        if (!string.IsNullOrWhiteSpace(shaderInfo))
        {
            throw new ShaderException($"Error Compiling {type} {name}: {shaderInfo}");
        }

        return shader;
    }
}