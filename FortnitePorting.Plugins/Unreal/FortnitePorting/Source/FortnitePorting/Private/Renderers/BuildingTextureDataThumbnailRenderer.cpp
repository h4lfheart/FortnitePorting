#include "Renderers/BuildingTextureDataThumbnailRenderer.h"
#include "Classes/BuildingTextureData.h"
#include "CanvasItem.h"
#include "CanvasTypes.h"
#include "Engine/Texture.h"
#include "Materials/MaterialInstance.h"
#include "ThumbnailRendering/ThumbnailManager.h"

void UBuildingTextureDataThumbnailRenderer::Draw(UObject* Object, int32 X, int32 Y, uint32 Width, uint32 Height, FRenderTarget* RenderTarget, FCanvas* Canvas, bool bAdditionalViewFamily)
{
    UBuildingTextureData* TextureData = Cast<UBuildingTextureData>(Object);
    if (!TextureData)
    {
        return;
    }

    UTexture* TextureToRender = nullptr;
    
    // Try to use Diffuse texture first
    if (TextureData->Diffuse && TextureData->Diffuse->GetResource())
    {
        TextureToRender = TextureData->Diffuse;
    }
    // Otherwise try to extract texture from OverrideMaterial
    else if (TextureData->OverrideMaterial)
    {
        // Try to get the base color texture from the material
        UTexture* MaterialTexture = nullptr;
        if (TextureData->OverrideMaterial->GetTextureParameterValue(FName("BaseColor"), MaterialTexture) ||
            TextureData->OverrideMaterial->GetTextureParameterValue(FName("Diffuse"), MaterialTexture) ||
            TextureData->OverrideMaterial->GetTextureParameterValue(FName("Albedo"), MaterialTexture))
        {
            TextureToRender = MaterialTexture;
        }
    }

    if (TextureToRender && TextureToRender->GetResource())
    {
        // Draw the texture
        Canvas->DrawTile(
            X, Y, Width, Height,
            0.0f, 0.0f, 1.0f, 1.0f,
            FLinearColor::White,
            TextureToRender->GetResource(),
            false
        );
    }
    else
    {
        // Draw a placeholder if no valid texture is found
        FCanvasTileItem TileItem(FVector2D(X, Y), FVector2D(Width, Height), FLinearColor(0.2f, 0.2f, 0.2f, 1.0f));
        TileItem.BlendMode = SE_BLEND_Opaque;
        Canvas->DrawItem(TileItem);

        // Draw text indicating no texture
        FCanvasTextItem TextItem(FVector2D(X + Width * 0.5f, Y + Height * 0.5f), FText::FromString(TEXT("No Texture")), GEngine->GetSmallFont(), FLinearColor::White);
        TextItem.bCentreX = true;
        TextItem.bCentreY = true;
        Canvas->DrawItem(TextItem);
    }
}

bool UBuildingTextureDataThumbnailRenderer::CanVisualizeAsset(UObject* Object)
{
    UBuildingTextureData* TextureData = Cast<UBuildingTextureData>(Object);
    return TextureData != nullptr;
}