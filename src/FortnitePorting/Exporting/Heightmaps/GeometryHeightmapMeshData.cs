using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Exporting.Heightmaps;

public sealed record GeometryHeightmapMeshGeometry(FVector[] Vertices, uint[] Indices);

public sealed record GeometryHeightmapMeshInstance(
    string Name,
    string Path,
    GeometryHeightmapMeshGeometry Geometry,
    FTransform Transform,
    bool IsTerrain);
