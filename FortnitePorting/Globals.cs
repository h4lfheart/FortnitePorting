global using static FortnitePorting.Services.ApplicationService;
using System;
using Avalonia.Platform.Storage;
using CUE4Parse.UE4.Objects.Core.Misc;
using FortnitePorting.Shared.Models;

namespace FortnitePorting;

public static class Globals
{
    public static string VersionString => Version.GetDisplayString(EVersionStringType.IdentifierPrefix);
    public static readonly FPVersion Version = new(3, 0, 7, 1);
    public const string OnlineTag = "FortnitePorting OG";
    
    public static readonly FilePickerFileType MappingsFileType = new("Unreal Mappings") { Patterns = [ "*.usmap" ] };
    public static readonly FilePickerFileType JSONFileType = new("JSON") { Patterns = [ "*.json" ] };
    public static readonly FilePickerFileType MP3FileType = new("MP3 Audio") { Patterns = [ "*.mp3" ] };
    public static readonly FilePickerFileType WAVFileType = new("WAV Audio") { Patterns = [ "*.wav" ] };
    public static readonly FilePickerFileType OGGFileType = new("OGG Audio") { Patterns = [ "*.ogg" ] };
    public static readonly FilePickerFileType FLACFileType = new("FLAC Audio") { Patterns = [ "*.flac" ] };
    public static readonly FilePickerFileType PNGFileType = new("PNG Image") { Patterns = [ "*.png" ] };
    public static readonly FilePickerFileType GIFFileType = new("GIF Image") { Patterns = [ "*.gif" ] };
    public static readonly FilePickerFileType ImageFileType = new("Image") { Patterns = [ "*.png", "*.jpg", "*.jpeg", "*.tga" ] };
    public static readonly FilePickerFileType PlaylistFileType = new("Fortnite Porting Playlist") { Patterns = [ "*.fp.playlist" ] };
    public static readonly FilePickerFileType ChatAttachmentFileType = new("Image") { Patterns = [ "*.png", "*.jpg", "*.jpeg" ] };
    public static readonly FilePickerFileType BlenderFileType = new("Blender") { Patterns = ["blender.exe"] };
    public static readonly FilePickerFileType UnrealProjectFileType = new("Unreal Project") { Patterns = ["*.uproject"] };
    
    public static readonly FGuid ZERO_GUID = new();
    public const string ZERO_CHAR = "0x0000000000000000000000000000000000000000000000000000000000000000";
    
    public const string DISCORD_URL = "https://discord.gg/FortnitePorting";
    public const string TWITTER_URL = "https://twitter.com/FortnitePorting";
    public const string GITHUB_URL = "https://github.com/h4lfheart/FortnitePorting";
    public const string KOFI_URL = "https://ko-fi.com/halfuwu";

    public static string GetSeededOGProfileURL(object? obj)
    {
        return OGProfileURLs[Math.Abs(obj?.GetHashCode() ?? 0) % 7];
    }
    
    public static readonly string[] OGProfileURLs = 
    [
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-001-Athena-Commando-F-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-002-Athena-Commando-F-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-003-Athena-Commando-F-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-004-Athena-Commando-F-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-005-Athena-Commando-M-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-006-Athena-Commando-M-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-007-Athena-Commando-M-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-008-Athena-Commando-M-L.png",
    ];
    
    public static string GetSeededOGStaffProfileURL(object? obj)
    {
        return OGStaffProfileURLs[Math.Abs(obj?.GetHashCode() ?? 0) % 2];
    }
    
    public static readonly string[] OGStaffProfileURLs = 
    [
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-017-Athena-Commando-M-L.png",
        "https://fortniteporting.halfheart.dev/OG/T-Soldier-HID-028-Athena-Commando-F-L.png"
    ];
}