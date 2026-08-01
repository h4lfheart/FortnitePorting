using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Rendering.Animation.Montage;
using FortnitePorting.Rendering.Extensions;

namespace FortnitePorting.Rendering.Animation.Pose;

public partial class SkeletalPoseEvaluator
{
    private readonly string[] _boneNames;
    private readonly Dictionary<string, int> _boneNameToIndex;
    private readonly FTransform[] _refLocalPose;
    private readonly int[] _parentIndices;
    private readonly Matrix4[] _inverseBindPose;
    private readonly FTransform[] _modelPose;
    private readonly Matrix4[] _modelMatrices;
    private readonly Matrix4[] _skinMatrices;
    private readonly FTransform[] _localPose;
    private int[] _trackRemap = [];

    public List<AnimMontageSection> MontageSections = [];
    private int _montageSectionIndex;

    public int BoneCount => _refLocalPose.Length;
    public Matrix4[] SkinMatrices => _skinMatrices;

    public float Time;
    public float Speed = 1f;
    public bool IsPlaying = true;
    public bool Loop = true;

    public CAnimSequence? Sequence { get; private set; }
    public float AnimStartTime { get; private set; }
    public float AnimEndTime { get; private set; }
    public float PlayRate { get; private set; } = 1f;

    public string? CurrentSectionName =>
        MontageSections.Count > 0 && _montageSectionIndex < MontageSections.Count
            ? MontageSections[_montageSectionIndex].Name
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
        _boneNameToIndex = new Dictionary<string, int>(boneCount, StringComparer.OrdinalIgnoreCase);
        _refLocalPose = new FTransform[boneCount];
        _parentIndices = new int[boneCount];
        _inverseBindPose = new Matrix4[boneCount];
        _modelPose = new FTransform[boneCount];
        _modelMatrices = new Matrix4[boneCount];
        _skinMatrices = new Matrix4[boneCount];
        _localPose = new FTransform[boneCount];
        _trackRemap = new int[boneCount];
        Array.Fill(_trackRemap, -1);

        for (var i = 0; i < boneCount; i++)
        {
            var bone = refSkeleton[i];
            _boneNames[i] = bone.Name.Text;
            _boneNameToIndex[_boneNames[i]] = i;
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
            var bindMatrix = _modelPose[i].ToMatrix4();
            _modelMatrices[i] = bindMatrix;
            _inverseBindPose[i] = Matrix4.Invert(bindMatrix);
            _skinMatrices[i] = Matrix4.Identity;
        }
    }

    public bool TryGetBoneMatrix(string boneName, out Matrix4 matrix)
    {
        if (_boneNameToIndex.TryGetValue(boneName, out var index))
        {
            matrix = _modelMatrices[index];
            return true;
        }

        matrix = Matrix4.Identity;
        return false;
    }

    public bool TryGetBoneTransform(string boneName, out FTransform transform)
    {
        if (_boneNameToIndex.TryGetValue(boneName, out var index))
        {
            transform = (FTransform) _modelPose[index].Clone();
            return true;
        }

        transform = FTransform.Identity;
        return false;
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
        _trackRemap = CreateIdentityTrackRemap(BoneCount, sequence.Tracks.Count);
    }

    public void Play(UAnimationAsset animation, USkeleton animationSkeleton, bool loop = true, float speed = 1f)
    {
        ClearMontageState();
        Speed = speed;
        IsPlaying = true;
        Time = 0f;

        if (animation is UAnimMontage montage)
        {
            MontageSections = BuildMontageSections(montage, animationSkeleton, _boneNames);
            if (MontageSections.Count == 0)
                throw new InvalidOperationException("Montage produced no playable sections.");

            ActivateMontageSection(0);
            return;
        }

        var animationSet = animation switch
        {
            UAnimSequence animSequence => animationSkeleton.ConvertAnims(animSequence),
            UAnimComposite composite => animationSkeleton.ConvertAnims(composite),
            _ => throw new ArgumentException($"Unsupported animation type: {animation.GetType().Name}")
        };

        if (animationSet.Sequences.Count == 0)
            throw new InvalidOperationException("Animation asset produced no sequences.");

        var sequence = animationSet.Sequences[0];
        _trackRemap = CreateTrackRemap(_boneNames, animationSkeleton, sequence);
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

    public void Pause() => IsPlaying = false;

    public void Resume()
    {
        if (Sequence is not null)
            IsPlaying = true;
    }

    public void Seek(float timeSeconds)
    {
        if (Sequence is null)
            return;

        var duration = Duration;
        Time = duration > 1e-6f ? Math.Clamp(timeSeconds, 0f, duration) : 0f;
        SampleCurrent();
    }

    public void JumpToSection(int index)
    {
        if (index < 0 || index >= MontageSections.Count)
            return;

        var wasPlaying = IsPlaying;
        ActivateMontageSection(index);
        IsPlaying = wasPlaying;
        Time = 0f;
        SampleCurrent();
    }

    public void JumpToSection(string name)
    {
        var index = MontageSections.FindIndex(section =>
            section.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            JumpToSection(index);
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
                 && (MontageSections.Count == 0 || !TryAdvanceMontageSection()))
        {
            Time = duration;
            IsPlaying = false;
        }
        else if (Time < 0f)
        {
            Time = 0f;
        }

        SampleCurrent();
    }

    public void SampleCurrent()
    {
        if (Sequence is null)
            return;

        var rate = Math.Abs(PlayRate) < 1e-6f ? 1f : Math.Abs(PlayRate);
        var sequenceTime = AnimStartTime + Time * rate;
        sequenceTime = Math.Clamp(sequenceTime, AnimStartTime, AnimEndTime);

        var sequenceLength = AnimPlaybackRange.SequenceLength(Sequence);
        if (sequenceLength <= 1e-6f || Sequence.NumFrames <= 0)
        {
            Evaluate(0f);
            return;
        }

        // NumFrames is exclusive at the end — GetBoneTransform indexes keys with frame/frameCount*keyCount,
        // so frame == NumFrames overflows the key array (common when non-looping playback hits Duration).
        var maxFrame = Math.Max(Sequence.NumFrames - 1e-3f, 0f);
        var frame = Math.Clamp(sequenceTime / sequenceLength * Sequence.NumFrames, 0f, maxFrame);
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
            var localTransform = (FTransform) _refLocalPose[boneIndex].Clone();
            var trackIndex = boneIndex < _trackRemap.Length ? _trackRemap[boneIndex] : -1;

            if (trackIndex >= 0 && trackIndex < trackCount)
            {
                var track = Sequence.Tracks[trackIndex];
                if (track.HasKeys())
                {
                    var rotation = localTransform.Rotation;
                    var position = localTransform.Translation;
                    var scale = localTransform.Scale3D.Equals(FVector.ZeroVector) ? FVector.OneVector : localTransform.Scale3D;
                    track.GetBoneTransform(frame, frameCount, ref rotation, ref position, ref scale);
                    if (scale.Equals(FVector.ZeroVector))
                        scale = FVector.OneVector;
                    rotation.Normalize();
                    localTransform = new FTransform(rotation, position, scale);
                }
            }

            localTransform.Normalize();
            _localPose[boneIndex] = localTransform;
        }

        BuildModelSpace(_localPose, _modelPose);

        for (var i = 0; i < BoneCount; i++)
        {
            var poseMatrix = _modelPose[i].ToMatrix4();
            _modelMatrices[i] = poseMatrix;
            _skinMatrices[i] = _inverseBindPose[i] * poseMatrix;
        }
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
