using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Export;

public static class ExportTypes
{
    public class Mesh
    {
        public string Path;
        public int NumLods;
        public FVector Location = FVector.ZeroVector;
        public FRotator Rotation = FRotator.ZeroRotator;
        public FVector Scale = FVector.OneVector;
    }

    public class Part : Mesh
    {
        public string Type;
        public bool AttachToSocket;
        public string? Socket;
    }

    public class Material
    {
        public string Path;
        public string Name;
        public string? ParentName;
        public int Slot;
        public int Hash;
    }
}