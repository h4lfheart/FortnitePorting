using System.ComponentModel;

namespace FortnitePorting;

public enum EInstallType
{
    [Description("Local Installation (Faster)")]
    Local,

    [Description("Fortnite Live (Slower)")]
    Live
}

public enum EAssetType
{
    [Description("Outfits")] Outfit,

    [Description("Back Blings")] Backpack,

    [Description("Pickaxes")] Pickaxe,

    [Description("Gliders")] Glider,

    [Description("Pets")] Pet,

    [Description("Weapons")] Weapon,

    [Description("Emotes")] Dance,

    [Description("Vehicles")] Vehicle,

    [Description("Props")] Prop,

    [Description("Meshes")] Mesh,

    [Description("Music Packs")] Music,
    
    [Description("Invalid")] Invalid,
}

public enum EFortCustomPartType : byte
{
    Head = 0,
    Body = 1,
    Hat = 2,
    Backpack = 3,
    MiscOrTail = 4,
    Face = 5,
    Gameplay = 6,
    NumTypes = 7
}

public enum ECustomHatType : byte
{
    HeadReplacement,
    Cap,
    Mask,
    Helmet,
    Hat,
    None
}

public enum ERigType
{
    [Description("Default Rig")] Default,
    [Description("Tasty™ Rig")] Tasty
}

public enum ESortType
{
    [Description("Default")] Default,
    [Description("A-Z")] AZ,
    [Description("Season")] Season,
    [Description("Rarity")] Rarity,
    [Description("Series")] Series
}

public enum EUpdateMode
{
    [Description("Stable")] Stable,
    [Description("Experimental")] Experimental
}

public enum EFortCustomGender : byte
{
    Invalid = 0,
    Male = 1,
    Female = 2,
    Both = 3
}

public enum EImageType
{
    [Description("PNG (.png)")] PNG,
    [Description("Targa (.tga)")] TGA
}

public enum ETreeItemType
{
    Folder,
    Asset
}

public enum EAnimGender
{
    Male,
    Female
}