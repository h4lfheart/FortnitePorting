using CUE4Parse_Conversion.Animations.PSA;

namespace FortnitePorting.Rendering.Animation;

public readonly record struct AnimPlaybackRange(float StartTime, float EndTime, float PlayRate)
{
    public float Duration
    {
        get
        {
            var span = Math.Max(EndTime - StartTime, 0f);
            var rate = Math.Abs(PlayRate);
            if (rate < 1e-6f) rate = 1f;
            return span / rate;
        }
    }

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
