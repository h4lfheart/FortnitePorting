using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Rendering.Extensions;

namespace FortnitePorting.Rendering.Animation;

public class SkeletalPoseEvaluator
{
    private readonly string[] _boneNames;
    private readonly FTransform[] _refLocalPose;
    private readonly int[] _parentIndices;
    private readonly Matrix4[] _inverseBindPose;
    private readonly FTransform[] _modelPose;
    private readonly Matrix4[] _skinMatrices;
    private readonly FTransform[] _localPose;
    private int[] _trackRemap = [];

    private List<AnimMontageSection> _montageSections = [];
    private int _montageSectionIndex;

    public int BoneCount => _refLocalPose.Length;

    public Matrix4[] SkinMatrices => _skinMatrices;

    public CAnimSequence? Sequence { get; private set; }
    public float Time { get; set; }
    public float Speed { get; set; } = 1f;
    public bool IsPlaying { get; set; } = true;
    public bool Loop { get; set; } = true;

    public float AnimStartTime { get; private set; }
    public float AnimEndTime { get; private set; }

    public float PlayRate { get; private set; } = 1f;

    public string? CurrentSectionName =>
        _montageSections.Count > 0 && _montageSectionIndex < _montageSections.Count
            ? _montageSections[_montageSectionIndex].Name
            : null;

    public float Duration
    {
        get
        {
            var span = Math.Max(AnimEndTime - AnimStartTime, 0f);
            var rate = Math.Abs(PlayRate);
            if (rate < 1e-6f) rate = 1f;
            return span / rate;
        }
    }

    public SkeletalPoseEvaluator(List<CSkelMeshBone> refSkeleton, FTransform[]? refBonePose = null)
    {
        var boneCount = refSkeleton.Count;
        _boneNames = new string[boneCount];
        _refLocalPose = new FTransform[boneCount];
        _parentIndices = new int[boneCount];
        _inverseBindPose = new Matrix4[boneCount];
        _modelPose = new FTransform[boneCount];
        _skinMatrices = new Matrix4[boneCount];
        _localPose = new FTransform[boneCount];
        _trackRemap = new int[boneCount];
        Array.Fill(_trackRemap, -1);

        for (var i = 0; i < boneCount; i++)
        {
            var bone = refSkeleton[i];
            _boneNames[i] = bone.Name.Text;
            _parentIndices[i] = bone.ParentIndex;

            if (refBonePose is not null && i < refBonePose.Length)
                _refLocalPose[i] = (FTransform) refBonePose[i].Clone();
            else
                _refLocalPose[i] = new FTransform(bone.Orientation, bone.Position, FVector.OneVector);

            _refLocalPose[i].Normalize();
            _localPose[i] = (FTransform) _refLocalPose[i].Clone();
        }

        BuildModelSpace(_refLocalPose, _modelPose);
        for (var i = 0; i < boneCount; i++)
        {
            var bindGl = _modelPose[i].ToMatrix4();
            _inverseBindPose[i] = Matrix4.Invert(bindGl);
            _skinMatrices[i] = Matrix4.Identity;
        }
    }

    public void Play(CAnimSequence sequence, bool loop = true, float speed = 1f)
    {
        ClearMontageState();
        Sequence = sequence;
        Time = 0f;
        Loop = loop;
        Speed = speed;
        IsPlaying = true;
        ApplyPlaybackRange(AnimPlaybackRange.FromSequence(sequence));
        _trackRemap = AnimTrackRemapper.CreateIdentity(BoneCount, sequence.Tracks.Count);
    }

    public void Play(UAnimationAsset animation, USkeleton animSkeleton, bool loop = true, float speed = 1f)
    {
        ClearMontageState();
        Speed = speed;
        IsPlaying = true;
        Time = 0f;

        if (animation is UAnimMontage montage)
        {
            _montageSections = MontageSectionBuilder.Build(montage, animSkeleton, _boneNames);
            if (_montageSections.Count == 0)
                throw new InvalidOperationException("Montage produced no playable sections.");

            ActivateMontageSection(0);
            return;
        }

        var animSet = animation switch
        {
            UAnimSequence animSequence => animSkeleton.ConvertAnims(animSequence),
            UAnimComposite composite => animSkeleton.ConvertAnims(composite),
            _ => throw new ArgumentException($"Unsupported animation type: {animation.GetType().Name}")
        };

        if (animSet.Sequences.Count == 0)
            throw new InvalidOperationException("Animation asset produced no sequences.");

        var sequence = animSet.Sequences[0];
        _trackRemap = AnimTrackRemapper.Create(_boneNames, animSkeleton, sequence);
        Sequence = sequence;
        Loop = loop;
        ApplyPlaybackRange(AnimPlaybackRange.FromSequence(sequence));
    }

    public void Stop()
    {
        IsPlaying = false;
        Sequence = null;
        Time = 0f;
        ClearMontageState();
        Array.Fill(_skinMatrices, Matrix4.Identity);
    }

    public void Update(float deltaTime)
    {
        if (!IsPlaying || Sequence is null)
            return;

        var duration = Duration;
        if (duration <= 1e-6f)
            return;

        Time += deltaTime * Speed;

        if (Loop)
        {
            Time %= duration;
            if (Time < 0f) Time += duration;
        }
        else if (Time >= duration
                 && (_montageSections.Count == 0 || !TryAdvanceMontageSection()))
        {
            Time = duration;
            IsPlaying = false;
        }
        else if (Time < 0f)
        {
            Time = 0f;
        }

        var rate = Math.Abs(PlayRate) < 1e-6f ? 1f : Math.Abs(PlayRate);
        var sequenceTime = AnimStartTime + Time * rate;
        sequenceTime = Math.Clamp(sequenceTime, AnimStartTime, AnimEndTime);

        var sequenceLength = AnimPlaybackRange.SequenceLength(Sequence);
        var frame = sequenceTime / sequenceLength * Sequence.NumFrames;
        Evaluate(frame);
    }

    public void Evaluate(float frame)
    {
        if (Sequence is null)
        {
            Array.Fill(_skinMatrices, Matrix4.Identity);
            return;
        }

        var frameCount = Math.Max(Sequence.NumFrames, 1);
        var trackCount = Sequence.Tracks.Count;

        for (var boneIndex = 0; boneIndex < BoneCount; boneIndex++)
        {
            var local = (FTransform) _refLocalPose[boneIndex].Clone();
            var trackIndex = boneIndex < _trackRemap.Length ? _trackRemap[boneIndex] : -1;

            if (trackIndex >= 0 && trackIndex < trackCount)
            {
                var track = Sequence.Tracks[trackIndex];
                if (track.HasKeys())
                {
                    var rotation = local.Rotation;
                    var position = local.Translation;
                    var scale = local.Scale3D.Equals(FVector.ZeroVector) ? FVector.OneVector : local.Scale3D;
                    track.GetBoneTransform(frame, frameCount, ref rotation, ref position, ref scale);
                    if (scale.Equals(FVector.ZeroVector))
                        scale = FVector.OneVector;
                    rotation.Normalize();
                    local = new FTransform(rotation, position, scale);
                }
            }

            local.Normalize();
            _localPose[boneIndex] = local;
        }

        BuildModelSpace(_localPose, _modelPose);

        for (var i = 0; i < BoneCount; i++)
        {
            var poseGl = _modelPose[i].ToMatrix4();
            _skinMatrices[i] = _inverseBindPose[i] * poseGl;
        }
    }

    private void ClearMontageState()
    {
        _montageSections = [];
        _montageSectionIndex = 0;
    }

    private void ActivateMontageSection(int index)
    {
        _montageSectionIndex = index;
        var section = _montageSections[index];
        Sequence = section.Sequence;
        _trackRemap = section.TrackRemap;
        AnimStartTime = section.AnimStartTime;
        AnimEndTime = section.AnimEndTime;
        PlayRate = section.PlayRate;
        Loop = section.Loop;
        Time = 0f;
        IsPlaying = true;
    }

    private bool TryAdvanceMontageSection()
    {
        var next = _montageSectionIndex + 1;
        if (next >= _montageSections.Count)
            return false;

        ActivateMontageSection(next);
        return true;
    }

    private void ApplyPlaybackRange(AnimPlaybackRange range)
    {
        AnimStartTime = range.StartTime;
        AnimEndTime = range.EndTime;
        PlayRate = range.PlayRate;
    }

    private void BuildModelSpace(FTransform[] localPose, FTransform[] outModel)
    {
        for (var i = 0; i < localPose.Length; i++)
        {
            var parent = _parentIndices[i];
            if (parent >= 0)
            {
                localPose[i].Normalize();
                outModel[parent].Normalize();
                outModel[i] = localPose[i] * outModel[parent];
            }
            else
            {
                outModel[i] = (FTransform) localPose[i].Clone();
                outModel[i].Normalize();
            }
        }
    }
}
