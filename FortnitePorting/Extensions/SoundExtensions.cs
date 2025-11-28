using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.GameTypes.FN.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Shared.Extensions;
using Log = Serilog.Log;

namespace FortnitePorting.Extensions;

public static class SoundExtensions
{
    
    public static bool TrySaveSoundToPath(USoundWave soundWave, string path)
    {
        soundWave.Decode(true, out var format, out var data);
        if (data is null) soundWave.Decode(false, out format, out data);
        if (data is null) return false;

        switch (format.ToLower())
        {
            case "adpcm":
                SaveADPCMAsWav(data, path);
                break;
            case "binka":
                SaveBinkaAsWav(data, path);
                break;
            case "rada":
                SaveRadaAsWav(data, path);
                break;
        }

        return true;
    }
    
    public static bool TrySaveSoundToPath(USoundWave soundWave, string path, out Stream stream)
    {
        if (File.Exists(path) || TrySaveSoundToPath(soundWave, path))
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return true;
        }

        stream = null;
        return false;
    }
    
    public static bool TrySaveSoundToAssets(USoundWave soundWave, string assetsRoot, out string path)
    {
        path = Path.Combine(assetsRoot, CUE4ParseExtensions.GetCleanedExportPath(soundWave) + ".wav");
        Directory.CreateDirectory(path.SubstringBeforeLast("/"));
        
        if (File.Exists(path) || TrySaveSoundToPath(soundWave, path))
        {
            return true;
        }

        return false;
    }
    
    public static bool TrySaveSoundToAssets(USoundWave soundWave, string assetsRoot, out Stream stream)
    {
        var path = Path.Combine(assetsRoot, CUE4ParseExtensions.GetCleanedExportPath(soundWave) + ".wav");
        Directory.CreateDirectory(path.SubstringBeforeLast("/"));
        
        if (File.Exists(path) || TrySaveSoundToPath(soundWave, path))
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return true;
        }

        stream = null;
        return false;
    }
    
    public static void SaveBinkaAsWav(byte[] data, string outPath)
    {
        var binkaPath = Path.ChangeExtension(outPath, "binka");
        File.WriteAllBytes(binkaPath, data);

        using (var binkaProcess = new Process())
        {
            binkaProcess.StartInfo = new ProcessStartInfo
            {
                FileName = Dependencies.BinkaDecoderFile.FullName,
                Arguments = $"-i \"{binkaPath}\" -o \"{outPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            binkaProcess.Start();
            binkaProcess.WaitForExit();
        }
        
        MiscExtensions.TryDeleteFile(binkaPath);
    }
    
    public static void SaveRadaAsWav(byte[] data, string outPath)
    {
        var radaPath = Path.ChangeExtension(outPath, "rada");
        File.WriteAllBytes(radaPath, data);

        using (var radaProcess = new Process())
        {
            radaProcess.StartInfo = new ProcessStartInfo
            {
                FileName = Dependencies.RadaDecoderFile.FullName,
                Arguments = $"-i \"{radaPath}\" -o \"{outPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            radaProcess.Start();
            radaProcess.WaitForExit();
            
            Log.Information(radaProcess.StandardOutput.ReadToEnd());
        }
        
        MiscExtensions.TryDeleteFile(radaPath);
    }
    
    public static void SaveADPCMAsWav(byte[] data, string outPath)
    {
        var adpcmPath = Path.ChangeExtension(outPath, "adpcm");
        File.WriteAllBytes(adpcmPath, data);

        using (var adpcmProcess = new Process())
        {
            adpcmProcess.StartInfo = new ProcessStartInfo
            {
                FileName = Dependencies.VgmStreamFile.FullName,
                Arguments = $"-o \"{outPath}\" \"{adpcmPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            adpcmProcess.Start();
            adpcmProcess.WaitForExit();
        }
        
        MiscExtensions.TryDeleteFile(adpcmPath);
    }
    
    extension(USoundCue root)
    {
        public List<Sound> HandleSoundTree(float offsetTime = 0.0f)
        {
            return root.FirstNode is null ? [] : HandleSoundTree(root.FirstNode.Load<USoundNode>(), offsetTime);
        }
    }

    extension(USoundNode? node)
    {
        public List<Sound> HandleSoundTree(float offsetTime = 0.0f)
        {
            var sounds = new List<Sound>();
            switch (node)
            {
                case USoundNodeWavePlayer player:
                {
                    sounds.Add(CreateSound(player, offsetTime));
                    break;
                }
                case USoundNodeDelay delay:
                {
                    foreach (var nodeObject in delay.ChildNodes) sounds.AddRange(HandleSoundTree(nodeObject.Load<USoundNode>(), offsetTime + delay.GetOrDefault("DelayMin", delay.GetOrDefault<float>("DelayMax"))));

                    break;
                }
                case USoundNodeRandom random:
                {
                    var index = Random.Shared.Next(0, random.ChildNodes.Length);
                    sounds.AddRange(HandleSoundTree(random.ChildNodes[index].Load<USoundNode>(), offsetTime));
                    break;
                }

                case UFortSoundNodeLicensedContentSwitcher switcher:
                {
                    sounds.AddRange(HandleSoundTree(switcher.ChildNodes.Last().Load<USoundNode>(), offsetTime));
                    break;
                }
                case USoundNodeDialoguePlayer dialoguePlayer:
                {
                    var dialogueWaveParameter = dialoguePlayer.Get<FStructFallback>("DialogueWaveParameter");
                    var dialogueWave = dialogueWaveParameter.Get<UDialogueWave>("DialogueWave");
                    var contextMappings = dialogueWave.Get<FStructFallback[]>("ContextMappings");
                    var soundWave = contextMappings.First().Get<FPackageIndex>("SoundWave");
                    sounds.Add(CreateSound(soundWave));
                    break;
                }
                case not null:
                {
                    foreach (var nodeObject in node.ChildNodes) sounds.AddRange(HandleSoundTree(nodeObject.Load<USoundNode>(), offsetTime));

                    break;
                }
            }

            return sounds;
        }
    }
    
    
    public static Sound CreateSound(USoundNodeWavePlayer player, float timeOffset = 0)
    {
        return new Sound(player.SoundWave, timeOffset, player.GetOrDefault("bLooping", false));
    }

    public static Sound CreateSound(FPackageIndex soundWave, float timeOffset = 0)
    {
        return new Sound(soundWave, timeOffset, false);
    }
}

public class Sound
{
    public FPackageIndex SoundWave;
    public float Time;
    public bool Loop;

    public Sound(FPackageIndex soundWave, float time, bool loop)
    {
        SoundWave = soundWave;
        Time = time;
        Loop = loop;
    }
}