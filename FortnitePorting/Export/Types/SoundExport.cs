using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using Serilog;

namespace FortnitePorting.Export.Types;

public class SoundExport : BaseExport
{
    public List<ExportSound> Sounds = [];
    
    public SoundExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
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