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

    public static MemoryStream ConvertBinkaToWav(byte[] data)
    {
        var binkaPath = Path.Combine(App.DataFolder.FullName, "temp.binka");
        File.WriteAllBytes(binkaPath, data);
        
        var binPath = Path.ChangeExtension(binkaPath, ".bin");
        var binkadecProcess = Process.Start(new ProcessStartInfo
        {
            FileName = DependencyService.BinkadecFile.FullName,
            Arguments = $"-i \"{binkaPath}\" -o \"{binPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        });
        binkadecProcess!.WaitForExit();
        File.Delete(binkaPath);

        var binkadecOutput = binkadecProcess.StandardOutput.ReadToEnd();
        var parameters = binkadecOutput.SubstringAfter("ffplay ").Trim();
        var wavPath = Path.ChangeExtension(binkaPath, ".wav");
        var ffmpegProcess = Process.Start(new ProcessStartInfo
        {
            FileName = DependencyService.FFmpegFile.FullName,
            Arguments = $"{parameters} \"{wavPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        });
        ffmpegProcess!.WaitForExit();
        File.Delete(binPath);
        
        // TODO cache in actual folder w/ filestream
        var wavStream = new MemoryStream(File.ReadAllBytes(wavPath));
        File.Delete(wavPath);

        return wavStream;
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
    
    public bool IsValid()
    {
        return SoundWave is not null && Time >= 0;
    }
}