using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Rendering.Extensions;

public static class OpenTKExtensions
{
    private static readonly Matrix4 BasisC = new(
        0.01f, 0f, 0f, 0f,
        0f, 0f, 0.01f, 0f,
        0f, 0.01f, 0f, 0f,
        0f, 0f, 0f, 1f);

    private static readonly Matrix4 BasisCInv = new(
        100f, 0f, 0f, 0f,
        0f, 0f, 100f, 0f,
        0f, 100f, 0f, 0f,
        0f, 0f, 0f, 1f);

    extension(FVector vector)
    {
        public Vector3 ToVector3()
        {
            return new Vector3(vector.X, vector.Z, vector.Y);
        }
    }

    extension(ref FTransform transform)
    {
        public void Normalize()
        {
            if (transform.Rotation.IsNormalized) return;

            var rotation = transform.Rotation;
            rotation.Normalize();
            transform.Rotation = rotation;
        }

        public Matrix4 ToMatrix4()
        {
            var m = transform.ToMatrixWithScale();
            var ue = new Matrix4(
                m.M00, m.M01, m.M02, m.M03,
                m.M10, m.M11, m.M12, m.M13,
                m.M20, m.M21, m.M22, m.M23,
                m.M30, m.M31, m.M32, m.M33);

            return BasisCInv * ue * BasisC;
        }
    }
    
    extension(Matrix4 matrix)
    {
        public FMatrix ToFMatrix()
        {
            return new FMatrix(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            );
        }
    }
}