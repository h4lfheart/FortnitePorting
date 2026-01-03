using CUE4Parse_Conversion.Meshes.PSK;
using FortnitePorting.RenderingX.Data.Programs;

namespace FortnitePorting.RenderingX.Renderers;

public class StaticMeshRenderer : MeshRenderer
{
    private static readonly ShaderProgram _shader = new("shader");
    
    public StaticMeshRenderer(CStaticMesh staticMesh, int lodLevel = 0) : base(_shader)
    {
        var lod = staticMesh.LODs[Math.Min(lodLevel, staticMesh.LODs.Count - 1)];
        
        Indices = lod.Indices!.Value;
        
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
    }

    protected override void BuildMesh()
    {
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Tangent", 3, VertexAttribPointerType.Float);
        RegisterAttribute("TexCoord", 2, VertexAttribPointerType.Float);
        RegisterAttribute("MaterialLayer", 1, VertexAttribPointerType.Float);
        
        base.BuildMesh();
    }
}