using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CUE4Parse.GameTypes.FN.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Services;

namespace FortnitePorting.Extensions;

public static class SoundExtensions
{
    private static readonly MMDeviceEnumerator DeviceEnumerator = new();
    
    public static ISoundOut GetSoundOut()
    {
        if (WasapiOut.IsSupportedOnCurrentPlatform)
        {
            return new WasapiOut { Device = DeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console) };
        }

        return new DirectSoundOut();
    }

    public static FileStream ConvertBinkaToWav(byte[] data, string name = "temp")
    {
        var wavPath = Path.Combine(App.AudioCacheFolder.FullName, $"{name}.wav");
        if (File.Exists(wavPath))
        {
            return new FileStream(wavPath, FileMode.Open, FileAccess.Read);
        }
        
        var binkaPath = Path.ChangeExtension(wavPath, "binka");
        var binPath = Path.ChangeExtension(binkaPath, ".bin");
        File.WriteAllBytes(binkaPath, data);

        string ffmpegParameters;
        using (var binkaProcess = new Process())
        {
         
            binkaProcess.StartInfo = new ProcessStartInfo
            {
                FileName = DependencyService.BinkadecFile.FullName,
                Arguments = $"-i \"{binkaPath}\" -o \"{binPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            binkaProcess.Start();
            binkaProcess.WaitForExit(5000);
            ffmpegParameters = binkaProcess.StandardOutput.ReadToEnd().SubstringAfter("ffplay ").Trim();
        }

        using (var ffmpegProcess = new Process())
        {
            ffmpegProcess.StartInfo = new ProcessStartInfo
            {
                FileName = DependencyService.FFmpegFile.FullName,
                Arguments = $"{ffmpegParameters} \"{wavPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit(5000);
        }
        
        File.Delete(binkaPath);
        File.Delete(binPath);
        
        return new FileStream(wavPath, FileMode.Open, FileAccess.Read);
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
                foreach (var nodeObject in delay.ChildNodes)
                {
                    sounds.AddRange(HandleSoundTree(nodeObject.Load<USoundNode>(), offsetTime + delay.GetOrDefault("DelayMin", delay.GetOrDefault<float>("DelayMax"))));
                }

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
                var soundWave = contextMappings.First().Get<USoundWave>("SoundWave");
                sounds.Add(CreateSound(soundWave));
                break;
            }
            case { } generic:
            {
                foreach (var nodeObject in generic.ChildNodes)
                {
                    sounds.AddRange(HandleSoundTree(nodeObject.Load<USoundNode>(), offsetTime));
                }

                break;
            }
        }

        return sounds;
    }
    
    private static Sound CreateSound(USoundNodeWavePlayer player, float timeOffset = 0)
    {
        var soundWave = player.SoundWave?.Load<USoundWave>();
        return new Sound(soundWave, timeOffset, player.GetOrDefault("bLooping", false));
    }

    private static Sound CreateSound(USoundWave soundWave, float timeOffset = 0)
    {
        return new Sound(soundWave, timeOffset, false);
    }
}

public class Sound
{
    public USoundWave? SoundWave;
    public float Time;
    public bool Loop;

    public Sound(USoundWave? soundWave, float time, bool loop)
    {
        SoundWave = soundWave;
        Time = time;
        Loop = loop;
    }
}