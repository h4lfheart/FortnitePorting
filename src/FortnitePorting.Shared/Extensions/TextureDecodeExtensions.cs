using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Shared.Options;

namespace FortnitePorting.Shared.Extensions;

public static class TextureDecodeExtensions
{
    extension(UTexture texture)
    {
        public CTexture? Decode(CancellationToken cancellationToken = default)
            => texture.Decode(SharedExportOptions.Default, cancellationToken);

        public CTexture? Decode(int maxMipSize, CancellationToken cancellationToken = default)
            => texture.Decode(maxMipSize, SharedExportOptions.Default, cancellationToken);

        public CTexture? DecodeMip(int mipIndex, int zLayer = 0, CancellationToken cancellationToken = default)
            => texture.DecodeMip(mipIndex, SharedExportOptions.Default, zLayer, cancellationToken);
    }

    extension(UTexture2DArray texture)
    {
        public CTexture[]? DecodeTextureArray(CancellationToken cancellationToken = default)
            => texture.DecodeTextureArray(SharedExportOptions.Default, cancellationToken);

        public CTexture[]? DecodeTextureArray(int mipIndex, CancellationToken cancellationToken = default)
            => texture.DecodeTextureArray(mipIndex, SharedExportOptions.Default, cancellationToken);

        public CTexture[]? DecodeTextureArray(FTexture2DMipMap? mip, CancellationToken cancellationToken = default)
            => texture.DecodeTextureArray(mip, SharedExportOptions.Default, cancellationToken);
    }
}
