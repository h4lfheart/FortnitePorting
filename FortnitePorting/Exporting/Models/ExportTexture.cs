using CUE4Parse.UE4.Assets.Exports.Texture;

namespace FortnitePorting.Exporting.Models;

public record ExportTexture(string Path, bool sRGB, TextureCompressionSettings CompressionSettings);