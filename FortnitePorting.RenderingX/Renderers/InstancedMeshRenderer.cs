using CUE4Parse_Conversion.Meshes.PSK;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Data.Buffers;
using FortnitePorting.RenderingX.Data.Programs;

namespace FortnitePorting.RenderingX.Renderers;

public class InstancedMeshRenderer : MeshRenderer
{
    protected Buffer<float> InstanceBuffer;

    private readonly List<TransformComponent> _transforms = [];
    
    public InstancedMeshRenderer()
    {
        Shader = new ShaderProgram("shader_inst");
    }

    public InstancedMeshRenderer(CStaticMesh staticMesh) : this()
    {
        var lod = staticMesh.LODs[0];
        
        var indices = lod.Indices.Value;
        Indices = new uint[indices.Length];
        for (var i = 0; i < indices.Length; i++)
        {
            Indices[i] = (uint) indices[i];
        }

        var vertices = lod.Verts;
        var buildVertices = new List<float>();
        foreach (var vertex in vertices)
        {
            var position = vertex.Position * 0.01f;
            var normal = vertex.Normal;

            buildVertices.AddRange([
                position.X, position.Z, position.Y,
                normal.X, normal.Z, normal.Y
            ]);
        }

        Vertices = buildVertices.ToArray();
    }

    public void AddTransform(TransformComponent transform)
    {
        _transforms.Add(transform);
        
        UpdateInstanceBuffer();
    }

    public void UpdateInstanceBuffer()
    {
        var instanceData = new float[_transforms.Count * 16];
        for (var transformIndex = 0; transformIndex < _transforms.Count; transformIndex++)
        {
            var transform = _transforms[transformIndex];
            var matrix = transform.GetWorldMatrix();
            var offset = transformIndex * 16;
            
            instanceData[offset + 0] = matrix.M11;
            instanceData[offset + 1] = matrix.M21;
            instanceData[offset + 2] = matrix.M31;
            instanceData[offset + 3] = matrix.M41;
            
            instanceData[offset + 4] = matrix.M12;
            instanceData[offset + 5] = matrix.M22;
            instanceData[offset + 6] = matrix.M32;
            instanceData[offset + 7] = matrix.M42;
            
            instanceData[offset + 8] = matrix.M13;
            instanceData[offset + 9] = matrix.M23;
            instanceData[offset + 10] = matrix.M33;
            instanceData[offset + 11] = matrix.M43;
            
            instanceData[offset + 12] = matrix.M14;
            instanceData[offset + 13] = matrix.M24;
            instanceData[offset + 14] = matrix.M34;
            instanceData[offset + 15] = matrix.M44;
        }

        InstanceBuffer.Fill(instanceData, BufferUsage.DynamicDraw);
    }

    protected override void BuildMesh()
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        
        base.BuildMesh();
        
        InstanceBuffer = new Buffer<float>(BufferTarget.ArrayBuffer);
        InstanceBuffer.Generate();
        InstanceBuffer.Bind();

        var instanceStride = 16 * sizeof(float);
    
        for (var i = 0; i < 4; i++)
        {
            var attributeIndex = (uint)(VertexAttributes.Count + i);
            var offset = i * 4 * sizeof(float);
        
            GL.VertexAttribPointer(attributeIndex, 4, VertexAttribPointerType.Float, false, instanceStride, offset);
            GL.EnableVertexAttribArray(attributeIndex);
            GL.VertexAttribDivisor(attributeIndex, 1);
        }
        
        UpdateInstanceBuffer();
    }

    protected override void RenderShader(CameraComponent camera)
    {
        Shader.Use();
        
        Shader.SetMatrix4("uView", camera.GetViewMatrix());
        Shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        Shader.SetUniform3("uDirection", camera.Direction);
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        VertexArray.Bind();
        InstanceBuffer.Bind();
        GL.DrawElementsInstanced(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0, _transforms.Count);
    }
    
    public override void Destroy()
    {
        base.Destroy();
        
        InstanceBuffer.Delete();
    }
}