using System.Runtime.InteropServices;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using SkiaSharp;

namespace FortnitePorting.Rendering.Preview;

public static class MeshPreviewRenderer
{
    private const int PreviewSize = 128;
    private const float FieldOfViewDegrees = 45f;
    private const float FramePadding = 1.15f;
    private const float UnitScale = 0.01f;
    private const float Ambient = 0.22f;
    private const float DiffuseStrength = 0.78f;
    private const byte Albedo = 178;

    public static SKBitmap? TryRender(UObject asset)
    {
        return asset switch
        {
            UStaticMesh staticMesh => RenderStatic(staticMesh),
            USkeletalMesh skeletalMesh => RenderSkeletal(skeletalMesh),
            _ => null
        };
    }

    private static SKBitmap? RenderStatic(UStaticMesh mesh)
    {
        using var dto = new StaticMeshDto(mesh, EMeshQuality.Highest);
        return RenderDto(dto);
    }

    private static SKBitmap? RenderSkeletal(USkeletalMesh mesh)
    {
        using var dto = new SkeletalMeshDto(mesh, EMeshQuality.Highest, exportMorphTarget: false);
        return RenderDto(dto);
    }

    private static SKBitmap? RenderDto<TVertex>(MeshDto<TVertex> dto) where TVertex : struct, IMeshVertex
    {
        if (dto.LODs.Count == 0) return null;

        var lod = dto.LODs[0];
        if (lod.Vertices.Length == 0 || lod.Indices.Length < 3) return null;

        var positions = new Vector3[lod.Vertices.Length];
        var normals = new Vector3[lod.Vertices.Length];
        for (var i = 0; i < lod.Vertices.Length; i++)
        {
            
            var vertex = lod.Vertices[i];
            var position = vertex.Position;
            positions[i] = new Vector3(position.X * UnitScale, position.Z * UnitScale, position.Y * UnitScale);
            normals[i] = new Vector3(vertex.Normal.X, vertex.Normal.Z, vertex.Normal.Y).Normalized();
        }

        var (view, projection, lightDir) = MeshPreviewRasterizer.CreateCamera(positions, FieldOfViewDegrees, FramePadding);
        return Rasterize(positions, normals, lod.Indices, view, projection, lightDir);
    }

    private static SKBitmap? Rasterize(
        Vector3[] positions,
        Vector3[] normals,
        uint[] indices,
        Matrix4 view,
        Matrix4 projection,
        Vector3 lightDir)
    {
        var colors = new byte[PreviewSize * PreviewSize * 4];
        var depths = new float[PreviewSize * PreviewSize];
        Array.Fill(depths, float.PositiveInfinity);

        var viewProjection = view * projection;
        var drawn = 0;

        for (var tri = 0; tri + 2 < indices.Length; tri += 3)
        {
            var i0 = indices[tri];
            var i1 = indices[tri + 1];
            var i2 = indices[tri + 2];
            if (i0 >= positions.Length || i1 >= positions.Length || i2 >= positions.Length) continue;

            var p0 = positions[i0];
            var p1 = positions[i1];
            var p2 = positions[i2];
            if (Vector3.Cross(p1 - p0, p2 - p0).LengthSquared < 1e-12f) continue;

            var n0 = normals[i0];
            var n1 = normals[i1];
            var n2 = normals[i2];

            var avgNormal = n0 + n1 + n2;
            if (avgNormal.LengthSquared < 1e-12f) continue;
            avgNormal.Normalize();
            if (Vector3.Dot(avgNormal, lightDir) < 0f)
            {
                n0 = -n0;
                n1 = -n1;
                n2 = -n2;
                avgNormal = -avgNormal;
            }

            if (Vector3.Dot(avgNormal, lightDir) <= 0.02f) continue;

            if (!MeshPreviewRasterizer.Project(p0, viewProjection, PreviewSize, out var s0, out var z0) ||
                !MeshPreviewRasterizer.Project(p1, viewProjection, PreviewSize, out var s1, out var z1) ||
                !MeshPreviewRasterizer.Project(p2, viewProjection, PreviewSize, out var s2, out var z2))
            {
                continue;
            }

            MeshPreviewRasterizer.DrawTriangle(
                colors, depths, PreviewSize,
                s0, z0, n0, s1, z1, n1, s2, z2, n2,
                lightDir, Albedo, Ambient, DiffuseStrength);
            drawn++;
        }

        if (drawn == 0) return null;

        var bitmap = new SKBitmap(PreviewSize, PreviewSize, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        Marshal.Copy(colors, 0, bitmap.GetPixels(), colors.Length);
        return bitmap;
    }
}
