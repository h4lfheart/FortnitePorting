using Avalonia;

namespace FortnitePorting.Shared.Extensions;

public static class MatrixExtensions
{
    public static double ScaleX(this Matrix matrix)
    {
        return matrix.M11;
    }
    
    public static double ScaleY(this Matrix matrix)
    {
        return matrix.M22;
    }
    
    public static double OffsetX(this Matrix matrix)
    {
        return matrix.M31;
    }
    
    public static double OffsetY(this Matrix matrix)
    {
        return matrix.M32;
    }
    
    public static Matrix WithScale(this Matrix matrix, float scaleX, float scaleY)
    {
        return new Matrix(
            scaleX, matrix.M12, matrix.M13,
            matrix.M21, scaleY, matrix.M23,
            matrix.M31, matrix.M32, matrix.M33
        );
    }
    
    public static Matrix WithOffset(this Matrix matrix, float offsetX, float offsetY)
    {
        return new Matrix(
            matrix.M11, matrix.M12, matrix.M13,
            matrix.M21, matrix.M22, matrix.M23,
            offsetX, offsetY, matrix.M33
        );
    }
}