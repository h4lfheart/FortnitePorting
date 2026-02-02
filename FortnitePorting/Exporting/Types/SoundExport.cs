using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;

namespace FortnitePorting.Exporting.Types;

public class SoundExport : BaseExport
{
    public List<ExportSound> Sounds = [];
    
    public SoundExport(string name, UObject asset, EExportType exportType, ExportDataMeta metaData) : base(name, exportType, metaData)
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
                    var soundWave = sound.SoundWave.Load<USoundWave>();
                    if (soundWave is null) continue;
                    
                    exportSounds.Add(soundWave);
                }
                
                break;
            }
            
            // TODO metasounds
        }
        
        foreach (var exportSound in exportSounds)
        {
            Sounds.Add(new ExportSound { Path = Exporter.Export(exportSound) });
        }
    }
    
}