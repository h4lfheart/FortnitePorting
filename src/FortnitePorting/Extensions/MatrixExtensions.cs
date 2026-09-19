using Avalonia;

namespace FortnitePorting.Extensions;

public static class MatrixExtensions
{
    extension(Matrix matrix)
    {
        public double ScaleX()
        {
            return matrix.M11;
        }

        public double ScaleY()
        {
            return matrix.M22;
        }

        public double OffsetX()
        {
            return matrix.M31;
        }

        public double OffsetY()
        {
            return matrix.M32;
        }

        public Matrix WithScale(float scaleX, float scaleY)
        {
            return new Matrix(
                scaleX, matrix.M12, matrix.M13,
                matrix.M21, scaleY, matrix.M23,
                matrix.M31, matrix.M32, matrix.M33
            );
        }

        public Matrix WithOffset(float offsetX, float offsetY)
        {
            return new Matrix(
                matrix.M11, matrix.M12, matrix.M13,
                matrix.M21, matrix.M22, matrix.M23,
                offsetX, offsetY, matrix.M33
            );
        }
    }
}