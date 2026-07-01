namespace FortnitePorting.Models.Fortnite;

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

public enum EFortCustomBodyType : byte
{
    None = 0,
    Small = 1,
    Medium = 2,
    MediumAndSmall = 3,
    Large = 4,
    LargeAndSmall = 5,
    LargeAndMedium = 6,
    All = 7
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

public enum EFortCustomGender : byte
{
    Invalid,
    Male,
    Female,
    Both
}