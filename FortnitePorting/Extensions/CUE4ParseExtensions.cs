using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.CUE4Parse.Extensions;

namespace FortnitePorting.Extensions;

public static class CUE4ParseExtensions
{
    extension(UObject asset)
    {
        public T? GetVehicleMetadata<T>(params string[] names) where T : class
        {
            FStructFallback? GetMarkerDisplay(UBlueprintGeneratedClass? blueprint)
            {
                var obj = blueprint?.ClassDefaultObject.Load();
                return obj?.GetOrDefault<FStructFallback>("MarkerDisplay");
            }

            var output = asset.GetAnyOrDefault<T?>(names);
            if (output is not null) return output;

            var vehicle = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass");
            output = GetMarkerDisplay(vehicle)?.GetAnyOrDefault<T?>(names);
            if (output is not null) return output;

            var vehicleSuper = vehicle.SuperStruct.Load<UBlueprintGeneratedClass>();
            output = GetMarkerDisplay(vehicleSuper)?.GetAnyOrDefault<T?>(names);
            return output;
        }

        public Bitmap? GetEditorIconBitmap()
        {
            var typeName = asset switch
            {
                UBuildingTextureData => "DataAsset",
                _ => asset.GetType().Name[1..]
            };

            typeName = typeName.Replace("EditorOnlyData", string.Empty);

            var filePath = $"avares://FortnitePorting/Assets/Unreal/{typeName}_64x.png";
            return !AssetLoader.Exists(new Uri(filePath)) ? null : ImageExtensions.AvaresBitmap(filePath);
        }
    }
}
