using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FortnitePorting.Exporting.Models;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
    public ExportAnimSection? AnimSequence(UAnimSequenceBase? animSequence, float time = 0.0f)
    {
        if (animSequence is null) return null;
        var exportSequence = new ExportAnimSection
        {
            Path = Export(animSequence),
            Name = animSequence.Name,
            Length = animSequence.SequenceLength,
            Time = time
        };

        return exportSequence;
    }
    
    public ExportAnimSection? AnimSequence(UAnimSequence? additiveSequence, UAnimSequence? baseSequence, float time = 0.0f)
    {
        if (additiveSequence is null) return null;
        if (baseSequence is null) return null;
        
        additiveSequence.RefPoseSeq = new ResolvedLoadedObject(baseSequence);

        var exportSequence = new ExportAnimSection
        {
            Path = Export(additiveSequence),
            Name = additiveSequence.Name,
            Length = additiveSequence.SequenceLength,
            Time = time
        };

        return exportSequence;
    }
}