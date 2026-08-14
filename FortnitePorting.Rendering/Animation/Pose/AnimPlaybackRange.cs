using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;

namespace FortnitePorting.Rendering.Animation.Pose;

public readonly record struct AnimPlaybackRange(float StartTime, float EndTime, float PlayRate)
{
    public static AnimPlaybackRange FromSequence(CAnimSequence sequence)
    {
        var source = sequence.OriginalSequence;
        var length = source.SequenceLength > 1e-6f ? source.SequenceLength : sequence.NumFrames / 30f;

        var endTime = sequence.AnimEndTime > 1e-6f ? sequence.AnimEndTime : length;
        endTime = Math.Clamp(endTime, 0f, length);

        var rateScale = source.RateScale;
        var playRate = Math.Abs(rateScale) < 1e-6f ? 1f : rateScale;
        return new AnimPlaybackRange(0f, endTime, playRate);
    }

    public static float SequenceLength(CAnimSequence sequence)
    {
        var length = sequence.OriginalSequence.SequenceLength;
        return length > 1e-6f ? length : sequence.NumFrames / 30f;
    }
}
