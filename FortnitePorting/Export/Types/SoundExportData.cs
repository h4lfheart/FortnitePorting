using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.Extensions;

namespace FortnitePorting.Export.Types;

public class SoundExportData : ExportDataBase
{
    public List<ExportSound> Sounds = [];
    
    public SoundExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportTargetType exportType) : base(name, asset, styles, type, EExportType.Sound, exportType)
    {
        var exportSounds = new List<USoundWave>();
        switch (asset)
        {
            case USoundWave soundWave:
            {
                exportSounds.Add(soundWave);
                break;
            }
            
            case USoundCue soundCue:
            {
                var sounds = soundCue.HandleSoundTree();
                foreach (var sound in sounds)
                {
                    exportSounds.Add(sound.SoundWave);
                }
                
                break;
            }
        }

        foreach (var exportSound in exportSounds)
        {
            if (exportType == EExportTargetType.Folder)
            {
                var exportPath = Exporter.Export(exportSound, true);
                Launch(Path.GetDirectoryName(exportPath)!);
            }
            else
            {
                Sounds.Add(new ExportSound { Path = Exporter.Export(exportSound) });
            }
        }
    }

}