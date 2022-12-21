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
    [Description("Outfits")]
    Outfit,
    
    [Description("Back Blings")]
    Backpack,
    
    [Description("Pickaxes")]
    Pickaxe,
    
    [Description("Gliders")]
    Glider,
    
    [Description("Weapons")]
    Weapon,

    [Description("Emotes")]
    Dance,
    
    [Description("Props")]
    Prop,
    
    [Description("Meshes")]
    Mesh,
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
    [Description("Default Rig")]
    Default,
    [Description("Tasty™ Rig")]
    Tasty
}

public enum ESortType
{
    [Description("Default")]
    Default,
    [Description("A-Z")]
    AZ,
    [Description("Season")]
    Season,
    [Description("Rarity")]
    Rarity,
    [Description("Series")]
    Series
}