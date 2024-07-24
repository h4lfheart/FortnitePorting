using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.GameTypes.FN.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Application;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;

namespace FortnitePorting.Extensions;

public static class SoundExtensions
{
    private static readonly MMDeviceEnumerator DeviceEnumerator = new();

    public static ISoundOut GetSoundOut()
    {
        try
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform) return new WasapiOut { Device = DeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) };

            return new DirectSoundOut();
        }
        catch (Exception e)
        {
            HandleException(e);
            return new DirectSoundOut();
        }
    }

    public static bool TrySaveAudio(USoundWave soundWave, string path)
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
        }

        return true;
    }
    
    public static bool TrySaveAudioStream(USoundWave soundWave, string path, out Stream stream)
    {
        if (TrySaveAudio(soundWave, path))
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
                FileName = DependencyService.BinkaFile.FullName,
                Arguments = $"-i \"{binkaPath}\" -o \"{outPath}\"",
                UseShellExecute = false
            };

            binkaProcess.Start();
            binkaProcess.WaitForExit();
        }
        
        TryDeleteFile(binkaPath);
    }
    
    public static void SaveADPCMAsWav(byte[] data, string outPath)
    {
        var adpcmPath = Path.ChangeExtension(outPath, "adpcm");
        File.WriteAllBytes(adpcmPath, data);

        using (var adpcmProcess = new Process())
        {
            adpcmProcess.StartInfo = new ProcessStartInfo
            {
                FileName = DependencyService.VGMStreamFile.FullName,
                Arguments = $"-o \"{outPath}\" \"{adpcmPath}\"",
                UseShellExecute = false
            };

            adpcmProcess.Start();
            adpcmProcess.WaitForExit();
        }
        
        TryDeleteFile(adpcmPath);
    }

    public static List<Sound> HandleSoundTree(this USoundCue root, float offsetTime = 0.0f)
    {
        if (root.FirstNode is null) return (List<Sound>) Enumerable.Empty<Sound>();
        return HandleSoundTree(root.FirstNode.Load<USoundNode>());
    }

    public static List<Sound> HandleSoundTree(this USoundNode? root, float offsetTime = 0.0f)
    {
        var sounds = new List<Sound>();
        switch (root)
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
            case { } generic:
            {
                foreach (var nodeObject in generic.ChildNodes) sounds.AddRange(HandleSoundTree(nodeObject.Load<USoundNode>(), offsetTime));

                break;
            }
        }

        return sounds;
    }

    private static Sound CreateSound(USoundNodeWavePlayer player, float timeOffset = 0)
    {
        return new Sound(player.SoundWave, timeOffset, player.GetOrDefault("bLooping", false));
    }

    private static Sound CreateSound(FPackageIndex soundWave, float timeOffset = 0)
    {
        return new Sound(soundWave, timeOffset, false);
    }

    private static bool TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
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