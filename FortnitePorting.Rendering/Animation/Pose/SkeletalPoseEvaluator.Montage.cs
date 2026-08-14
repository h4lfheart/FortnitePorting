using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FortnitePorting.Rendering.Animation.Montage;

namespace FortnitePorting.Rendering.Animation.Pose;

public partial class SkeletalPoseEvaluator
{
    private void ClearMontageState()
    {
        MontageSections = [];
        _montageSectionIndex = 0;
    }

    private void ActivateMontageSection(int index)
    {
        _montageSectionIndex = index;
        var montageSection = MontageSections[index];
        Sequence = montageSection.Sequence;
        _trackRemap = montageSection.TrackRemap;
        AnimStartTime = montageSection.AnimStartTime;
        AnimEndTime = montageSection.AnimEndTime;
        PlayRate = montageSection.PlayRate;
        Loop = montageSection.Loop;
        Time = 0f;
        IsPlaying = true;
        SampleCurrent();
    }

    private bool TryAdvanceMontageSection()
    {
        var nextSectionIndex = _montageSectionIndex + 1;
        if (nextSectionIndex >= MontageSections.Count)
            return false;

        ActivateMontageSection(nextSectionIndex);
        return true;
    }

    private static List<AnimMontageSection> BuildMontageSections(
        UAnimMontage montage, USkeleton animationSkeleton, string[] meshBoneNames)
    {
        var montageSections = new List<AnimMontageSection>();
        if (montage.CompositeSections is not { Length: > 0 })
            return montageSections;

        TraverseSectionTree(montageSections, montage, animationSkeleton, meshBoneNames, montage.CompositeSections[0]);
        return montageSections;
    }

    private static void TraverseSectionTree(
        List<AnimMontageSection> montageSections,
        UAnimMontage montage,
        USkeleton animationSkeleton,
        string[] meshBoneNames,
        FCompositeSection currentSection)
    {
        var linkedSequence = currentSection.LinkedSequence.Load<UAnimSequence>();
        if (linkedSequence is null)
            return;

        var convertedSequence = animationSkeleton.ConvertAnims(linkedSequence).Sequences.FirstOrDefault();
        if (convertedSequence is null)
            return;

        var (animStartTime, animEndTime, playRate) = ResolveSectionPlayback(montage, currentSection, convertedSequence);
        var isLoopingSection = currentSection.SectionName == currentSection.NextSectionName || currentSection.NextSectionName.IsNone;

        montageSections.Add(new AnimMontageSection
        {
            Name = currentSection.SectionName.Text,
            Sequence = convertedSequence,
            AnimStartTime = animStartTime,
            AnimEndTime = animEndTime,
            PlayRate = playRate,
            Loop = isLoopingSection,
            TrackRemap = CreateTrackRemap(meshBoneNames, animationSkeleton, convertedSequence)
        });

        if (isLoopingSection)
            return;

        var nextSection = montage.CompositeSections.FirstOrDefault(section => currentSection.NextSectionName == section.SectionName);
        if (nextSection is null)
            return;

        if (montageSections.Any(section => section.Name.Equals(nextSection.SectionName.Text, StringComparison.OrdinalIgnoreCase)))
            return;

        TraverseSectionTree(montageSections, montage, animationSkeleton, meshBoneNames, nextSection);
    }

    private static (float AnimStart, float AnimEnd, float PlayRate) ResolveSectionPlayback(
        UAnimMontage montage,
        FCompositeSection section,
        CAnimSequence convertedSequence)
    {
        var sourceSequence = convertedSequence.OriginalSequence;
        var sequenceLength = AnimPlaybackRange.SequenceLength(convertedSequence);
        var sequenceRate = Math.Abs(sourceSequence.RateScale) < 1e-6f ? 1f : sourceSequence.RateScale;

        FAnimSegment? animSegment = null;
        if (section.SlotIndex >= 0
            && section.SlotIndex < montage.SlotAnimTracks.Length
            && section.SegmentIndex >= 0
            && section.SegmentIndex < montage.SlotAnimTracks[section.SlotIndex].AnimTrack.AnimSegments.Length)
        {
            animSegment = montage.SlotAnimTracks[section.SlotIndex].AnimTrack.AnimSegments[section.SegmentIndex];
        }
        else
        {
            animSegment = montage.SlotAnimTracks
                .SelectMany(slot => slot.AnimTrack.AnimSegments)
                .FirstOrDefault(segment =>
                    Math.Abs(segment.StartPos - section.SegmentBeginTime) < 0.01f
                    && segment.AnimReference.TryLoad<UAnimSequence>(out var animReference)
                    && (ReferenceEquals(animReference, sourceSequence) || animReference.Name == sourceSequence.Name));
        }

        if (animSegment is null)
            return (0f, sequenceLength, sequenceRate);

        var animStartTime = Math.Clamp(animSegment.AnimStartTime, 0f, sequenceLength);
        var animEndTime = animSegment.AnimEndTime > 1e-6f ? animSegment.AnimEndTime : sequenceLength;
        animEndTime = Math.Clamp(animEndTime, animStartTime, sequenceLength);

        var segmentPlayRate = Math.Abs(animSegment.AnimPlayRate) < 1e-6f ? 1f : animSegment.AnimPlayRate;
        return (animStartTime, animEndTime, sequenceRate * segmentPlayRate);
    }

    private static int[] CreateTrackRemap(string[] meshBoneNames, USkeleton animationSkeleton, CAnimSequence sequence)
    {
        var trackRemap = new int[meshBoneNames.Length];
        Array.Fill(trackRemap, -1);

        var animationBoneInfo = animationSkeleton.ReferenceSkeleton.FinalRefBoneInfo;
        var boneNameToTrackIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var animationBoneIndex = 0; animationBoneIndex < animationBoneInfo.Length; animationBoneIndex++)
        {
            if (animationBoneIndex >= sequence.Tracks.Count) break;
            if (!sequence.Tracks[animationBoneIndex].HasKeys()) continue;
            boneNameToTrackIndex[animationBoneInfo[animationBoneIndex].Name.Text] = animationBoneIndex;
        }

        for (var meshBoneIndex = 0; meshBoneIndex < meshBoneNames.Length; meshBoneIndex++)
        {
            if (boneNameToTrackIndex.TryGetValue(meshBoneNames[meshBoneIndex], out var trackIndex))
                trackRemap[meshBoneIndex] = trackIndex;
        }

        return trackRemap;
    }

    private static int[] CreateIdentityTrackRemap(int boneCount, int trackCount)
    {
        var trackRemap = new int[boneCount];
        for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
            trackRemap[boneIndex] = boneIndex < trackCount ? boneIndex : -1;
        return trackRemap;
    }
}
