using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;

namespace FortnitePorting.Rendering.Animation.Montage;

public sealed class AnimMontageSection
{
    public required string Name { get; init; }
    public required CAnimSequence Sequence { get; init; }
    public required float AnimStartTime { get; init; }
    public required float AnimEndTime { get; init; }
    public required float PlayRate { get; init; }
    public required bool Loop { get; init; }
    public required int[] TrackRemap { get; init; }
}
