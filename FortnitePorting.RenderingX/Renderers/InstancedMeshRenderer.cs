using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Data.Buffers;
using FortnitePorting.RenderingX.Data.Programs;
using FortnitePorting.RenderingX.Exceptions;
using FortnitePorting.RenderingX.Materials;

namespace FortnitePorting.RenderingX.Renderers;

public class InstancedMeshRenderer : MeshRenderer
{
    protected Buffer<float> InstanceBuffer;

    private readonly List<SpatialComponent> _transforms = [];
    
    public List<Section> Sections = [];
    public Material[] Materials = [];
    
    public InstancedMeshRenderer() : base(new ShaderProgram("shader_inst"))
    {
    }

    public InstancedMeshRenderer(UStaticMesh staticMesh, int lodLevel = 0) : this()
    {
        if (!staticMesh.TryConvert(out var convertedMesh))
        {
            return;
        }
        
        var lod = convertedMesh.LODs[Math.Min(lodLevel, convertedMesh.LODs.Count - 1)];
        
        var indices = lod.Indices!.Value;
        Indices = new uint[indices.Length];
        for (var i = 0; i < indices.Length; i++)
        {
            Indices[i] = (uint) indices[i];
        }

        var vertices = lod.Verts;
        var extraUVs = lod.ExtraUV.Value;
        var buildVertices = new List<float>();
        
        for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
        {
            var vertex = vertices[vertexIndex];
            var position = vertex.Position * 0.01f;
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;
            var uv = vertex.UV;
            var materialLayer = extraUVs.Length > 0 ? extraUVs[0][vertexIndex].U : 0;

            buildVertices.AddRange([
                position.X, position.Z, position.Y,
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y,
                uv.U, uv.V,
                materialLayer
            ]);
            
        }

        Vertices = buildVertices.ToArray();

        var sections = lod.Sections.Value;
        Materials = new Material[sections.Length];

        for (var sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            var section = sections[sectionIndex];
            Sections.Add(new Section(section.MaterialIndex, section.NumFaces * 3, section.FirstIndex));

            if (staticMesh.Materials[section.MaterialIndex]?.TryLoad(out var sectionMaterial) ?? false)
            {
                Materials[sectionIndex] = sectionMaterial switch
                {
                    UMaterialInstanceConstant materialInstance => new Material(materialInstance),
                    UMaterial material => new Material(material),
                    _ => new Material()
                };
            }
            else
            {
                Materials[sectionIndex] = new Material();
            }
        }
    }
    
    public void ClearTransforms()
    {
        _transforms.Clear();
    }

    public void AddTransform(SpatialComponent transform)
    {
        _transforms.Add(transform);
    }

    public void UpdateInstanceBuffer()
    {
        var instanceData = new float[_transforms.Count * 16];
        for (var transformIndex = 0; transformIndex < _transforms.Count; transformIndex++)
        {
            var transform = _transforms[transformIndex];
            var matrix = transform.WorldMatrix;
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

    public override void Initialize()
    {
        base.Initialize();
           
        foreach (var material in Materials)
        {
            Shader.Use();
            material.SetUniforms(Shader);
        }
    }

    protected override void BuildMesh()
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("MaterialLayer", 1, VertexAttribPointerType.Float);
        
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
        
        Shader.SetMatrix4("uView", camera.ViewMatrix());
        Shader.SetMatrix4("uProjection", camera.ProjectionMatrix());
        Shader.SetUniform3("fCameraDirection", camera.Direction);
        Shader.SetUniform3("fCameraPosition", camera.WorldPosition);
        
        foreach (var section in Sections)
        {
            Materials[section.MaterialIndex].Bind();
        }
    }

    protected override void RenderGeometry(CameraComponent camera)
    {
        VertexArray.Bind();
        InstanceBuffer.Bind();
        
        foreach (var section in Sections)
        {
            GL.DrawElementsInstanced(PrimitiveType.Triangles, section.FaceCount, DrawElementsType.UnsignedInt, section.FirstFaceIndexPtr, _transforms.Count);
        }
    }
    
    public override void Destroy()
    {
        base.Destroy();
        
        InstanceBuffer.Delete();
    }
}