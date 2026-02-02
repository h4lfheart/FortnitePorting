using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.RenderingX.Extensions;

public static class OpenTKExtensions
{
    extension(FVector vector)
    {
        public Vector3 ToVector3()
        {
            return new Vector3(vector.X, vector.Z, vector.Y);
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