using System;
using System.IO;
using System.Threading.Tasks;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.Utils;
using FortnitePorting;
using FortnitePorting.Application;
using FortnitePorting.Extensions;

using Serilog;
using SkiaSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.UObject;
using Flurl.Util;
using System.Linq;

abstract class AssetType
{
    public EAssetType Type = EAssetType.None;
    public string[] Classes = Array.Empty<string>();
    public string[] Filters = Array.Empty<string>();
    public Func<UObject, UTexture2D?> IconHandler = GetAssetIcon;
    public Func<UObject, FText?> DisplayNameHandler = asset => asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName") ?? new FText(asset.Name);
    public Func<UObject, FText> DescriptionHandler = asset => asset.GetAnyOrDefault<FText?>("Description", "ItemDescription") ?? new FText("No description.");
    public virtual Dictionary<string, float> StatsHandler(UObject asset) {
        return new Dictionary<string, float>();
    }

    public static UTexture2D? GetAssetIcon(UObject asset)
    {
        UTexture2D previewImage = null;
        if (asset.TryGetValue(out FInstancedStruct[] dataList, "DataList"))
        {
            foreach (var data in dataList)
            {
                if (data.NonConstStruct is not null && data.NonConstStruct.TryGetValue(out previewImage, "Icon", "LargeIcon")) break;
            }
        }

        previewImage ??= asset.GetAnyOrDefault<UTexture2D?>("Icon", "LargeIcon", "SmallPreviewImage", "LargePreviewImage");
        return previewImage;
    }
}

class Outfit : AssetType
{
    public Outfit()
    {
        Type = EAssetType.Outfit;
        Classes = new[] { "AthenaCharacterItemDefinition" };
        Filters = new[] { "_NPC", "_TBD", "CID_VIP", "_Creative", "_SG" };
        IconHandler = asset =>
        {
            asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
            if (previewImage is null && asset.TryGetValue(out UObject heroDef, "HeroDefinition"))
            {
                previewImage = GetAssetIcon(heroDef);
                previewImage ??= heroDef.GetAnyOrDefault<UTexture2D>("SmallPreviewImage", "LargePreviewImage");

            }
            previewImage ??= GetAssetIcon(asset);
            return previewImage;
        };
    }

}

class Backpack : AssetType
{
    public Backpack()
    {
        Type = EAssetType.Backpack;
        Classes = new[] { "AthenaBackpackItemDefinition" };
        Filters = new[] { "_STWHeroNoDefaultBackpack", "_TEST", "Dev_", "_NPC", "_TBD" };
    }
}

class Pickaxe : AssetType
{
    public Pickaxe()
    {
        Type = EAssetType.Pickaxe;
        Classes = new[] { "AthenaPickaxeItemDefinition" };
        Filters = new[] { "Dev_", "TBD_" };
        IconHandler = asset =>
        {
            asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
            if (asset.TryGetValue(out UObject heroDef, "WeaponDefinition"))
            {
                previewImage = GetAssetIcon(heroDef);
                previewImage ??= heroDef.GetAnyOrDefault<UTexture2D>("SmallPreviewImage", "LargePreviewImage");
            }
            previewImage ??= GetAssetIcon(asset);
            return previewImage;
        };
    }
}

class Glider : AssetType
{
    public Glider()
    {
        Type = EAssetType.Glider;
        Classes = new[] { "AthenaGliderItemDefinition" };
    }
}

class Pet : AssetType
{
    public Pet()
    {
        Type = EAssetType.Pet;
        Classes = new[] { "AthenaPetCarrierItemDefinition" };
    }
}

class Toy : AssetType
{
    public Toy()
    {
        Type = EAssetType.Toy;
        Classes = new[] { "AthenaToyItemDefinition" };
    }
}


class Emoticon : AssetType
{
    public Emoticon()
    {
        Type = EAssetType.Emoticon;
        Classes = new[] { "AthenaEmojiItemDefinition" };
        Filters = new[] { "Emoji_100APlus" };
    }
}

class Spray : AssetType
{
    public Spray()
    {
        Type = EAssetType.Spray;
        Classes = new[] { "AthenaSprayItemDefinition" };
        Filters = new[] { "SPID_000", "SPID_001" };
    }
}

class Banner : AssetType
{
    public Banner()
    {
        Type = EAssetType.Banner;
        Classes = new[] { "FortHomebaseBannerIconItemDefinition" };
    }
}

class LoadingScreen : AssetType
{
    public LoadingScreen()
    {
        Type = EAssetType.LoadingScreen;
        Classes = new[] { "AthenaLoadingScreenItemDefinition" };
    }
}

class Emote : AssetType
{
    public Emote()
    {
        Type = EAssetType.Emote;
        Classes = new[] { "AthenaDanceItemDefinition" };
        Filters = new[] { "_CT", "_NPC" };
    }
}

class Item : AssetType
{
    public Item()
    {
        Type = EAssetType.Item;
        Classes = new[] {
            "AthenaGadgetItemDefinition",
            "FortWeaponRangedItemDefinition",
            "FortWeaponMeleeItemDefinition",
            "FortCreativeWeaponMeleeItemDefinition",
            "FortCreativeWeaponRangedItemDefinition",
            "FortWeaponMeleeDualWieldItemDefinition"
        };
        Filters = new[] { "_Harvest", "Weapon_Pickaxe_", "Weapons_Pickaxe_", "Dev_WID" };
    }

    public override Dictionary<string, float> StatsHandler(UObject asset)
    {
        var stats = new Dictionary<string, float>();
        var hasHandle = asset.TryGetValue(out FStructFallback statHandle, "WeaponStatHandle");

        if (!hasHandle) return stats;

        var hasRow = statHandle.TryGetValue(out FName weaponRowName, "RowName");
        var hasTable = statHandle.TryGetValue(out UDataTable dataTable, "DataTable");

        if (!hasRow || !hasTable) return stats;

        var hasDataRow = dataTable.TryGetDataTableRow(weaponRowName.Text, StringComparison.OrdinalIgnoreCase, out var weaponRowValue);

        if (!hasDataRow) return stats;

        weaponRowValue.TryGetValue(out float firingRate, "FiringRate");
        stats.Add("firingRate", firingRate);

        weaponRowValue.TryGetValue(out float burstFiringRate, "BurstFiringRate");
        stats.Add("burstRateOfFire", burstFiringRate);

        weaponRowValue.TryGetValue(out float diceCritChance, "DiceCritChance");
        stats.Add("criticalHitChance", diceCritChance);

        weaponRowValue.TryGetValue(out float diceCritDamageMultiplier, "DiceCritDamageMultiplier");
        stats.Add("criticalHitDamageMultiplier", diceCritDamageMultiplier);

        weaponRowValue.TryGetValue(out float reloadTime, "ReloadTime");
        stats.Add("reloadTime", reloadTime);

        weaponRowValue.TryGetValue(out float dmgPB, "DmgPB");
        stats.Add("damagePerBullet", dmgPB);

        weaponRowValue.TryGetValue(out float envDmgPB, "EnvDmgPB");
        stats.Add("environmentDamagePerBullet", envDmgPB);

        weaponRowValue.TryGetValue(out float clipSize, "ClipSize");
        stats.Add("clipSize", clipSize);

        weaponRowValue.TryGetValue(out float ammoCostPerFire, "AmmoCostPerFire");
        stats.Add("ammoCostPerFire", ammoCostPerFire);

        weaponRowValue.TryGetValue(out float bulletsPerCartridge, "BulletsPerCartridge");
        stats.Add("bulletsPerCartridge", bulletsPerCartridge);
            
        return stats;
    }
}

class Resource : AssetType
{
    public Resource()
    {
        Type = EAssetType.Resource;
        Classes = new[] { "FortIngredientItemDefinition", "FortResourceItemDefinition" };
        Filters = new[] { "SurvivorItemData", "OutpostUpgrade_StormShieldAmplifier" };
    }
}

class Trap : AssetType
{
    public Trap()
    {
        Type = EAssetType.Trap;
        Classes = new[] { "FortTrapItemDefinition" };
        Filters = new[] { "TID_Creative", "TID_Floor_Minigame_Trigger_Plate" };
    }
}

class Vehicle : AssetType
{
    public Vehicle()
    {
        Type = EAssetType.Vehicle;
        Classes = new[] { "FortVehicleItemDefinition" };
        IconHandler = asset => GetVehicleInfo<UTexture2D>(asset, "SmallPreviewImage", "LargePreviewImage", "Icon");
        DisplayNameHandler = asset => GetVehicleInfo<FText>(asset, "DisplayName", "ItemName");
    }

    private static T? GetVehicleInfo<T>(UObject asset, params string[] names) where T : class
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
}

class UObjectConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var texture = (UObject) value;
        writer.WriteValue(texture.Name);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
    }

    public override bool CanRead
    {
        get { return false; }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(UTexture);
    }
}

class ExportAsset
{
    [JsonProperty("id")]
    public string ID { get; set; }
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }
    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("gameplayTags")]
    public FGameplayTagContainer? GameplayTags { get; set; }

    [JsonIgnore]
    public EFortRarity Rarity { get; set; }

    [JsonProperty("rarity")]
    public string RarityText { get; set; }

    [JsonProperty("season")]
    public int Season { get; set; }
    [JsonProperty("series")]
    public string Series { get; set; }

    [JsonProperty("icon"), JsonConverter(typeof(UObjectConverter))]
    public UTexture2D Icon { get; set; }

    [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
    public EAssetType Type {  get; set; }

    [JsonProperty("stats")]
    public Dictionary<string, float> Stats { get; set; }

    public ExportAsset(AssetType assetType, UObject asset) {
        var icon = assetType.IconHandler(asset) ?? throw new Exception("Icon not present for asset");
        Icon = icon;

        ID = asset.Name;
        var displayName = assetType.DisplayNameHandler(asset)?.Text;
        if (string.IsNullOrEmpty(displayName)) displayName = asset.Name;
        DisplayName = displayName;
        Description = assetType.DescriptionHandler(asset).Text;
        Rarity = asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
        RarityText = Rarity.GetNameText().Text;
        GameplayTags = asset.GetOrDefault<FGameplayTagContainer?>("GameplayTags");
        if (GameplayTags is null)
        {
            GameplayTags = asset.GetDataListItem<FGameplayTagContainer?>("Tags");
        }

        var seasonTag = GameplayTags?.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text;
        Season = int.TryParse(seasonTag?.SubstringAfterLast("."), out var seasonNumber) ? seasonNumber : int.MaxValue;

        var series = asset.GetOrDefault<UObject?>("Series");
        Series = series?.GetAnyOrDefault<FText>("DisplayName", "ItemName").Text ?? string.Empty;

        Type = assetType.Type;

        Stats = assetType.StatsHandler(asset);
    }

    public async Task Export()
    {
        var tasks = new List<Task> { ExportIcon(), ExportJSON() };

        await Task.WhenAll(tasks);
    }

    public Task ExportJSON()
    {
        var directory = Path.Combine(AppSettings.Current.GetExportPath(), "json", Type.ToString());
        Directory.CreateDirectory(directory);
        var exportPath = Path.Combine(directory, ID + ".json");

        return Task.Run(() =>
        {
            using (StreamWriter file = File.CreateText(exportPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this);
            }
        });
    }

    public Task ExportIcon()
    {
        Directory.CreateDirectory(AppSettings.Current.GetExportPath());

        var iconName = Icon.Name;

        var exportPath = Path.Combine(AppSettings.Current.GetExportPath(), iconName + ".webp");

        return Task.Run(() =>
        {
            try
            {
                // Log.Information("Exporting icon for asset: {Name}", ID);
                using var fileStream = File.OpenWrite(exportPath);
                var iconBitmap = Icon.Decode()!;
                SKPixmap pixmap = iconBitmap.PeekPixels();
                var options = new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100);
                pixmap.Encode(options).SaveTo(fileStream);
            }
            catch (IOException e)
            {
                Log.Warning("Failed to Export icon for asset: {Name}", ID);
                Log.Warning(e.Message + e.StackTrace);
            }
        });
    }
}