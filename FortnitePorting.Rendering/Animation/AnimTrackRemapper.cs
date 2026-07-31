using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace FortnitePorting.Rendering.Animation;

public static class AnimTrackRemapper
{
    public static int[] Create(string[] meshBoneNames, USkeleton animSkeleton, CAnimSequence sequence)
    {
        var remap = new int[meshBoneNames.Length];
        Array.Fill(remap, -1);

        var animBoneInfo = animSkeleton.ReferenceSkeleton.FinalRefBoneInfo;
        var nameToTrack = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var animBoneIndex = 0; animBoneIndex < animBoneInfo.Length; animBoneIndex++)
        {
            if (animBoneIndex >= sequence.Tracks.Count) break;
            if (!sequence.Tracks[animBoneIndex].HasKeys()) continue;
            nameToTrack[animBoneInfo[animBoneIndex].Name.Text] = animBoneIndex;
        }

        for (var meshBoneIndex = 0; meshBoneIndex < meshBoneNames.Length; meshBoneIndex++)
        {
            if (nameToTrack.TryGetValue(meshBoneNames[meshBoneIndex], out var trackIndex))
                remap[meshBoneIndex] = trackIndex;
        }

        return remap;
    }

    public static int[] CreateIdentity(int boneCount, int trackCount)
    {
        var remap = new int[boneCount];
        for (var i = 0; i < boneCount; i++)
            remap[i] = i < trackCount ? i : -1;
        return remap;
    }
}
