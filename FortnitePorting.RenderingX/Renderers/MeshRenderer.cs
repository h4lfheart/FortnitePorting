using System.Collections.Specialized;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Data.Buffers;
using FortnitePorting.RenderingX.Data.Programs;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;

namespace FortnitePorting.RenderingX.Renderers;

public class MeshRenderer(ShaderProgram shaderProgram) : Renderable
{
    public ShaderProgram Shader = shaderProgram;

    public MeshComponent Component;

    public float[] Vertices = [];
    public uint[] Indices = [];

    protected Buffer<float> VertexBuffer;
    protected Buffer<uint> IndexBuffer;
    protected VertexArray<float> VertexArray;

    public List<VertexAttribute> VertexAttributes = [];

    protected virtual void BuildMesh()
    {
        VertexArray = new VertexArray<float>();
        VertexArray.Generate();
        VertexArray.Bind();
        
        VertexBuffer = new Buffer<float>(BufferTarget.ArrayBuffer);
        VertexBuffer.Generate();
        VertexBuffer.Fill(Vertices);
        
        IndexBuffer = new Buffer<uint>(BufferTarget.ElementArrayBuffer);
        IndexBuffer.Generate();
        IndexBuffer.Fill(Indices);
        
        var stride = VertexAttributes.Sum(attr => attr.Size);
        for (int attributeIndex = 0, offset = 0; attributeIndex < VertexAttributes.Count; attributeIndex++)
        {
            var attribute = VertexAttributes[attributeIndex];
            VertexArray.VertexAttribPointer((uint) attributeIndex, attribute.Size, attribute.Type, stride, offset);
            offset += attribute.Size;
        }
    }

    protected virtual void RenderShader(CameraComponent camera)
    {
        Shader.Use();
        Shader.SetMatrix4("uTransform", Component.WorldMatrix);
        Shader.SetMatrix4("uView", camera.ViewMatrix());
        Shader.SetMatrix4("uProjection", camera.ProjectionMatrix());
        Shader.SetUniform3("fCameraDirection", camera.Direction);
        Shader.SetUniform3("fCameraPosition", camera.WorldPosition);
    }
    
    protected virtual void RenderGeometry(CameraComponent camera)
    {
        VertexArray.Bind();
        GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
    }
    
    public override void Initialize()
    {
        base.Initialize();
        
        Shader.Generate();
        Shader.Link();
        
        BuildMesh();
    }

    public override void Render(CameraComponent camera)
    {
        base.Render(camera);
        
        GL.Disable(EnableCap.CullFace);
        RenderShader(camera);
        RenderGeometry(camera);
        GL.Enable(EnableCap.CullFace);
        
    }

    public override void Destroy()
    {
        base.Destroy();
        
        VertexBuffer.Delete();
        IndexBuffer.Delete();
        VertexArray.Delete();
    }
    
    protected void RegisterAttribute(string name, int size, VertexAttribPointerType type)
    {
        VertexAttributes.Add(new VertexAttribute(name, size, type));
    }
}

public record VertexAttribute(string Name, int Size, VertexAttribPointerType Type);