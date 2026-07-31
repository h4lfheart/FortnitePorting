using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace FortnitePorting.Rendering.Animation;

public static class MontageSectionBuilder
{
    public static List<AnimMontageSection> Build(UAnimMontage montage, USkeleton animSkeleton, string[] meshBoneNames)
    {
        var sections = new List<AnimMontageSection>();
        if (montage.CompositeSections is not { Length: > 0 })
            return sections;

        TraverseSectionTree(sections, montage, animSkeleton, meshBoneNames, montage.CompositeSections[0]);
        return sections;
    }

    private static void TraverseSectionTree(
        List<AnimMontageSection> sections,
        UAnimMontage montage,
        USkeleton animSkeleton,
        string[] meshBoneNames,
        FCompositeSection currentSection)
    {
        var baseSequence = currentSection.LinkedSequence.Load<UAnimSequence>();
        if (baseSequence is null)
            return;

        var converted = animSkeleton.ConvertAnims(baseSequence).Sequences.FirstOrDefault();
        if (converted is null)
            return;

        var (animStart, animEnd, playRate) = ResolveSectionPlayback(montage, currentSection, converted);
        var loop = currentSection.SectionName == currentSection.NextSectionName || currentSection.NextSectionName.IsNone;

        sections.Add(new AnimMontageSection
        {
            Name = currentSection.SectionName.Text,
            Sequence = converted,
            AnimStartTime = animStart,
            AnimEndTime = animEnd,
            PlayRate = playRate,
            Loop = loop,
            TrackRemap = AnimTrackRemapper.Create(meshBoneNames, animSkeleton, converted)
        });

        if (loop)
            return;

        var nextSection = montage.CompositeSections.FirstOrDefault(sec => currentSection.NextSectionName == sec.SectionName);
        if (nextSection is null)
            return;

        if (sections.Any(section => section.Name.Equals(nextSection.SectionName.Text, StringComparison.OrdinalIgnoreCase)))
            return;

        TraverseSectionTree(sections, montage, animSkeleton, meshBoneNames, nextSection);
    }

    private static (float AnimStart, float AnimEnd, float PlayRate) ResolveSectionPlayback(
        UAnimMontage montage,
        FCompositeSection section,
        CAnimSequence converted)
    {
        var source = converted.OriginalSequence;
        var length = AnimPlaybackRange.SequenceLength(converted);
        var seqRate = Math.Abs(source.RateScale) < 1e-6f ? 1f : source.RateScale;

        FAnimSegment? segment = null;
        if (section.SlotIndex >= 0
            && section.SlotIndex < montage.SlotAnimTracks.Length
            && section.SegmentIndex >= 0
            && section.SegmentIndex < montage.SlotAnimTracks[section.SlotIndex].AnimTrack.AnimSegments.Length)
        {
            segment = montage.SlotAnimTracks[section.SlotIndex].AnimTrack.AnimSegments[section.SegmentIndex];
        }
        else
        {
            segment = montage.SlotAnimTracks
                .SelectMany(slot => slot.AnimTrack.AnimSegments)
                .FirstOrDefault(seg =>
                    Math.Abs(seg.StartPos - section.SegmentBeginTime) < 0.01f
                    && seg.AnimReference.TryLoad<UAnimSequence>(out var animReference)
                    && (ReferenceEquals(animReference, source) || animReference.Name == source.Name));
        }

        if (segment is null)
            return (0f, length, seqRate);

        var animStart = Math.Clamp(segment.AnimStartTime, 0f, length);
        var animEnd = segment.AnimEndTime > 1e-6f ? segment.AnimEndTime : length;
        animEnd = Math.Clamp(animEnd, animStart, length);
        
        var segRate = Math.Abs(segment.AnimPlayRate) < 1e-6f ? 1f : segment.AnimPlayRate;
        return (animStart, animEnd, seqRate * segRate);
    }
}
