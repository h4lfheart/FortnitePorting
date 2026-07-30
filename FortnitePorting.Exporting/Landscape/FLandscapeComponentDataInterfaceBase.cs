using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Models.Unreal.Landscape;

public class FLandscapeComponentDataInterfaceBase
{
    public int HeightmapStride = 0;
    public int HeightmapComponentOffsetX = 0;
    public int HeightmapComponentOffsetY = 0;
    public int HeightmapSubsectionOffset = 0;
    
    public const int MipLevel = 0;
    
    protected int ComponentSizeQuads = 0;
    protected int ComponentSizeVerts = 0;
    protected int SubsectionSizeVerts = 0;
    protected int ComponentNumSubsections = 0;

    public FLandscapeComponentDataInterfaceBase(ULandscapeComponent inComponent)
    {
	    var heightMapTexture = inComponent.HeightmapTexture;
	    HeightmapStride = heightMapTexture.PlatformData.SizeX;
	    HeightmapComponentOffsetX = (int) (heightMapTexture.PlatformData.SizeX * inComponent.HeightmapScaleBias.Z);
	    HeightmapComponentOffsetY = (int) (heightMapTexture.PlatformData.SizeY * inComponent.HeightmapScaleBias.W);
	    HeightmapSubsectionOffset = inComponent.SubsectionSizeQuads + 1;

	    ComponentSizeQuads = inComponent.ComponentSizeQuads;
	    ComponentSizeVerts = inComponent.ComponentSizeQuads + 1;
	    SubsectionSizeVerts = inComponent.SubsectionSizeQuads + 1;
	    ComponentNumSubsections = inComponent.NumSubsections;
    }
    
    public void VertexIndexToXY(int vertexIndex, out int outX, out int outY)
    {
        outX = vertexIndex % ComponentSizeVerts;
        outY = vertexIndex / ComponentSizeVerts;
    }

    public void QuadIndexToXY(int quadIndex, out int outX, out int outY)
    {
        outX = quadIndex % (ComponentSizeVerts-1);
        outY = quadIndex / (ComponentSizeVerts-1);
    }

    public int VertexXYToIndex(int vertX, int vertY)
    {
        return vertY * ComponentSizeVerts + vertX;
    }
    
    public void ComponentXYToSubsectionXY(int compX, int compY, out int subNumX, out int subNumY, out int subX, out int subY)
    {
        // We do the calculation as if we're looking for the previous vertex.
        // This allows us to pick up the last shared vertex of every subsection correctly.
        subNumX = (compX-1) / (SubsectionSizeVerts - 1);
        subNumY = (compY-1) / (SubsectionSizeVerts - 1);
        subX = (compX-1) % (SubsectionSizeVerts - 1) + 1;
        subY = (compY-1) % (SubsectionSizeVerts - 1) + 1;

        // If we're asking for the first vertex, the calculation above will lead
        // to a negative SubNumX/Y, so we need to fix that case up.
        if (subNumX < 0)
        {
            subNumX = 0;
            subX = 0;
        }

        if (subNumY < 0)
        {
            subNumY = 0;
            subY = 0;
        }
    }
    
    public void VertexXYToTexelXY(int vertX, int vertY, out int outX, out int outY)
	{
		ComponentXYToSubsectionXY(vertX, vertY, out var subNumX, out var subNumY, out var subX, out var subY);

		outX = subNumX * SubsectionSizeVerts + subX;
		outY = subNumY * SubsectionSizeVerts + subY;
	}
	
	public int VertexIndexToTexel(int vertexIndex)
	{
		VertexIndexToXY(vertexIndex, out var vertX, out var vertY);
		VertexXYToTexelXY(vertX, vertY, out var texelX, out var texelY);
		return TexelXYToIndex(texelX, texelY);
	}

	public int TexelXYToIndex(int texelX, int texelY)
	{
		return texelY * ComponentNumSubsections * SubsectionSizeVerts + texelX;
	}

	public ushort GetHeight(int localX, int localY, FColor[] heightAndNormals)
	{
		var texel = GetHeightData(localX, localY, heightAndNormals);
		return (ushort) ((texel.R << 8) + texel.G);
	}

	public float GetScaleFactor()
	{
		return (float) ComponentSizeQuads / (ComponentSizeVerts - 1);
	}

	public FVector GetLocalVertex(int localX, int localY, FColor[] heightAndNormals)
	{
		var scaleFactor = GetScaleFactor();
		
		return new FVector(localX * scaleFactor , localY * scaleFactor, GetLocalHeight(localX, localY, heightAndNormals));
	}

	public float GetLocalHeight(int localX, int localY, FColor[] heightAndNormals)
	{
		return LandscapeDataAccess.GetLocalHeight(GetHeight(localX, localY, heightAndNormals));
	}

	public FColor GetHeightData(int localX, int localY, FColor[] heightAndNormals)
	{
		VertexXYToTexelXY(localX, localY, out var texelX, out var texelY);

		return heightAndNormals[texelX + HeightmapComponentOffsetX + (texelY + HeightmapComponentOffsetY) * HeightmapStride];
	}

	public void GetLocalTangentVectors(int LocalX, int LocalY, out FVector localTangentX, out FVector localTangentY, out FVector localTangentZ, FColor[] heightAndNormals)
	{
		var data = GetHeightData(LocalX, LocalY, heightAndNormals);
		localTangentZ = LandscapeDataAccess.UnpackNormal(data);
		localTangentX = new FVector(-localTangentZ.Z, 0f, localTangentZ.X);
		localTangentY = new FVector(0f, localTangentZ.Z, -localTangentZ.Y);
	}

	public int GetComponentSizeVerts() => ComponentSizeVerts;
}