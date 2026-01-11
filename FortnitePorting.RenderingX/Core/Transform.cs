using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.RenderingX.Core;

public struct Transform
{
    public static Transform Identity => new();

    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Transform()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    public Transform(FTransform transform)
    {
        Position = new Vector3(transform.Translation.X, transform.Translation.Z, transform.Translation.Y) * 0.01f;
        Rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Z, transform.Rotation.Y, -transform.Rotation.W);
        Scale = new Vector3(transform.Scale3D.X, transform.Scale3D.Z, transform.Scale3D.Y);
    }
    
    public static implicit operator Transform(FTransform fTransform)
    {
        return new Transform(fTransform);
    }
}