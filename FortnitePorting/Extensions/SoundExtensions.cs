using System.Diagnostics;
using System.IO;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.Utils;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Extensions;

public class SoundExtensions
{
    public static bool TrySaveSound(USoundWave soundWave, string path)
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
    
    public static bool TrySaveSoundStream(USoundWave soundWave, out Stream stream)
    {
        var path = Path.Combine(AssetsFolder.FullName, MiscExtensions.GetCleanedExportPath(soundWave) + ".wav");
        Directory.CreateDirectory(path.SubstringBeforeLast("/"));
        
        if (File.Exists(path) || TrySaveSound(soundWave, path))
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return true;
        }

        stream = null;
        return false;
    }
    
    public static bool TrySaveSoundStream(USoundWave soundWave, string path, out Stream stream)
    {
        if (TrySaveSound(soundWave, path))
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
                UseShellExecute = false,
                CreateNoWindow = true
            };

            binkaProcess.Start();
            binkaProcess.WaitForExit();
        }
        
        MiscExtensions.TryDeleteFile(binkaPath);
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
                UseShellExecute = false,
                CreateNoWindow = true
            };

            adpcmProcess.Start();
            adpcmProcess.WaitForExit();
        }
        
        MiscExtensions.TryDeleteFile(adpcmPath);
    }
}