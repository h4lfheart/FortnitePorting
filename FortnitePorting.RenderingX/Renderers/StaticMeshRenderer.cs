using CUE4Parse_Conversion.Meshes.PSK;

namespace FortnitePorting.RenderingX.Renderers;

public class StaticMeshRenderer : MeshRenderer
{
    public StaticMeshRenderer(CStaticMesh staticMesh)
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

    protected override void BuildMesh()
    {
        
        RegisterAttribute("Position", 3, VertexAttribPointerType.Float);
        RegisterAttribute("Normal", 3, VertexAttribPointerType.Float);
        
        base.BuildMesh();
    }
}