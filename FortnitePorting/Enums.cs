using System.ComponentModel;

namespace FortnitePorting;

public enum EInstallType
{
    [Description("Local")]
    Local,
    
    [Description("Fortnite Live")]
    Live
}

public enum ERichPresenceAccess
{
    [Description("Always")]
    Always,
    
    [Description("Never")]
    Never
}

public enum EAssetType
{
    [Description("Outfits")]
    Outfit,
    
    [Description("Back Blings")]
    Backpack,
    
    [Description("Harvesting Tools")]
    Pickaxe,
    
    [Description("Gliders")]
    Glider,
    
    [Description("Weapons")]
    Weapon,

    [Description("Emotes")]
    Dance,
    
    /*[Description("Props")]
    Prop,
    
    [Description("Meshes")]
    Mesh,*/
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