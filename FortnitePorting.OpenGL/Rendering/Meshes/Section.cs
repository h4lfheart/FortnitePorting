using System.Numerics;

namespace FortnitePorting.OpenGL.Rendering.Meshes;

public class Section
{
    public readonly int MaterialIndex;
    public readonly int FaceCount;
    public readonly int FirstFaceIndex;
    public readonly IntPtr FirstFaceIndexPtr;

    public Section(int index, int faceCount, int firstFaceIndex)
    {
        MaterialIndex = index;
        FaceCount = faceCount;
        FirstFaceIndex = firstFaceIndex;
        FirstFaceIndexPtr = new IntPtr(FirstFaceIndex * sizeof(uint));
    }
}