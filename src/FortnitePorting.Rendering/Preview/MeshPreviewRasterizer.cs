namespace FortnitePorting.Rendering.Preview;

public static class MeshPreviewRasterizer
{
    public static (Matrix4 View, Matrix4 Projection, Vector3 LightDir) CreateCamera(
        Vector3[] positions,
        float fieldOfViewDegrees,
        float framePadding)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var position in positions)
        {
            min = Vector3.ComponentMin(min, position);
            max = Vector3.ComponentMax(max, position);
        }

        var center = (min + max) * 0.5f;
        var extent = (max - min) * 0.5f;
        var radius = MathF.Max(extent.Length, 1e-3f);
        var fovRadians = MathHelper.DegreesToRadians(fieldOfViewDegrees);
        var distance = radius / MathF.Tan(fovRadians * 0.5f) * framePadding;

        var direction = new Vector3(-1f, -1f, -1f).Normalized();
        var eye = center - direction * distance;
        var lightDir = (eye - center).Normalized();
        var view = Matrix4.LookAt(eye, center, Vector3.UnitY);
        var projection = Matrix4.CreatePerspectiveFieldOfView(fovRadians, 1f, 0.1f, 10000f);
        return (view, projection, lightDir);
    }

    public static bool Project(Vector3 world, Matrix4 viewProjection, int size, out Vector2 screen, out float depth)
    {
        var clip = new Vector4(world, 1f) * viewProjection;
        if (clip.W <= 1e-5f)
        {
            screen = default;
            depth = 0f;
            return false;
        }

        var invW = 1f / clip.W;
        var ndcX = clip.X * invW;
        var ndcY = clip.Y * invW;
        var ndcZ = clip.Z * invW;

        screen = new Vector2(
            (ndcX * 0.5f + 0.5f) * (size - 1),
            (1f - (ndcY * 0.5f + 0.5f)) * (size - 1));
        depth = ndcZ;
        return depth is >= -1f and <= 1f;
    }

    public static void DrawTriangle(
        byte[] colors,
        float[] depths,
        int size,
        Vector2 a, float za, Vector3 na,
        Vector2 b, float zb, Vector3 nb,
        Vector2 c, float zc, Vector3 nc,
        Vector3 lightDir,
        byte albedo,
        float ambient,
        float diffuseStrength)
    {
        var minX = (int) MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X)));
        var maxX = (int) MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X)));
        var minY = (int) MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y)));
        var maxY = (int) MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y)));

        minX = Math.Clamp(minX, 0, size - 1);
        maxX = Math.Clamp(maxX, 0, size - 1);
        minY = Math.Clamp(minY, 0, size - 1);
        maxY = Math.Clamp(maxY, 0, size - 1);

        var area = Edge(a, b, c);
        if (MathF.Abs(area) < 1e-5f) return;
        var invArea = 1f / area;

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            var w0 = Edge(b, c, p) * invArea;
            var w1 = Edge(c, a, p) * invArea;
            var w2 = Edge(a, b, p) * invArea;
            if (w0 < 0f || w1 < 0f || w2 < 0f) continue;

            var depth = w0 * za + w1 * zb + w2 * zc;
            var index = y * size + x;
            if (depth >= depths[index]) continue;
            depths[index] = depth;

            var normal = w0 * na + w1 * nb + w2 * nc;
            if (normal.LengthSquared > 1e-12f) normal.Normalize();

            var ndotl = MathF.Max(0f, Vector3.Dot(normal, lightDir));
            var shade = Math.Clamp(ambient + diffuseStrength * ndotl, 0f, 1f);
            var channel = (byte) Math.Clamp((int) (albedo * shade), 0, 255);

            var offset = index * 4;
            colors[offset] = channel;
            colors[offset + 1] = channel;
            colors[offset + 2] = channel;
            colors[offset + 3] = 255;
        }
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 c) =>
        (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
}
