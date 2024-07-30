using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace FortnitePorting.Models.Unreal.Landscape;

public unsafe class FLandscapeComponentDataInterface : FLandscapeComponentDataInterfaceBase
{
   public FColor[] HeightMipData;
   
   private ULandscapeComponent Component;
   
   private readonly ConcurrentDictionary<string, byte[]> LayerCache = [];
   
   private static int[] ChannelOffsets =
   [
	   (int) Marshal.OffsetOf<FColor>("R"),
	   (int) Marshal.OffsetOf<FColor>("G"), 
	   (int) Marshal.OffsetOf<FColor>("B"), 
	   (int) Marshal.OffsetOf<FColor>("A")
   ];

   public FLandscapeComponentDataInterface(ULandscapeComponent inComponent) : base(inComponent)
   {
      Component = inComponent;

      var heightMapTexture = Component.HeightmapTexture;
      
      var data = heightMapTexture.PlatformData.Mips[MipLevel].BulkData.Data;
      if (data is null) throw new ParserException("Heightmap bulk data is null, cannot continue with landscape inteface.");
         
      using var reader = new FByteArchive($"{inComponent.Name}_Height", data);
      HeightMipData = reader.ReadArray<FColor>(data.Length / sizeof(FColor));
   }

	private bool GetWeightmapTextureData(FWeightmapLayerAllocationInfo layerAllocation, out byte[] outData)
	{
		var layerName = layerAllocation.LayerInfo.Name.SubstringBefore("_LayerInfo");
		if (LayerCache.TryGetValue(layerName, out var cached))
		{
			outData = cached;
			return true;
		}
		
		var weightMapTexture = Component.WeightmapTextures[layerAllocation.WeightmapTextureIndex];

		var data = weightMapTexture.PlatformData.Mips[MipLevel].BulkData.Data;
		if (data is null) throw new ParserException("Weightmap bulk data is null, cannot continue with landscape inteface.");
         
		using var reader = new FByteArchive($"{Component.Name}_Weight_{layerName}", data);
		
		var dataCount = (int) Math.Pow((Component.SubsectionSizeQuads + 1) * Component.NumSubsections, 2);
		outData = new byte[dataCount];
		
		var offset = ChannelOffsets[layerAllocation.WeightmapTextureChannel];
		for (var pixelIndex = 0; pixelIndex < dataCount; pixelIndex++)
		{
			reader.Seek(pixelIndex * 4 + offset, SeekOrigin.Begin);
			outData[pixelIndex] = reader.Read<byte>();
		}

		LayerCache.TryAdd(layerName, outData);
		return true;
	}
	
	public byte GetLayerWeight(int vertX, int vertY, FWeightmapLayerAllocationInfo layerAllocation)
	{
		VertexXYToTexelXY(vertX, vertY, out var texelX, out var texelY);

		var weightmapTexture = Component.WeightmapTextures[layerAllocation.WeightmapTextureIndex];

		var weightmapStride = weightmapTexture.PlatformData.SizeX >> MipLevel;
		var weightmapComponentOffsetX = (int)((weightmapTexture.PlatformData.SizeX >> MipLevel) * Component.WeightmapScaleBias.Z);
		var weightmapComponentOffsetY = (int)((weightmapTexture.PlatformData.SizeY >> MipLevel) * Component.WeightmapScaleBias.W);

		return GetWeightmapTextureData(layerAllocation, out var data) ? data[texelX + weightmapComponentOffsetX + (texelY + weightmapComponentOffsetY) * weightmapStride] : byte.MinValue;
	}

}