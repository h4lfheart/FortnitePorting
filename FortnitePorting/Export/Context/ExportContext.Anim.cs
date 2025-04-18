using System.Linq;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Engine.Animation;
using FortnitePorting.Export.Models;
using Serilog;

namespace FortnitePorting.Export.Context;

public partial class ExportContext
{
    public ExportAnimSection? AnimSequence(UAnimSequence? animSequence, float time = 0.0f)
    {
        if (animSequence is null) return null;
        var exportSequence = new ExportAnimSection
        {
            Path = Export(animSequence),
            Name = animSequence.Name,
            Length = animSequence.SequenceLength,
            Time = time
        };
        
        var floatCurves = animSequence.CompressedCurveData.FloatCurves ?? [];
        foreach (var curve in floatCurves)
        {
            exportSequence.Curves.Add(new ExportCurve
            {
                Name = curve.CurveName.Text,
                Keys = curve.FloatCurve.Keys.Select(x => new ExportCurveKey(x.Time, x.Value)).ToList()
            });
        }

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
        
        var baseFloatCurves = baseSequence.CompressedCurveData.FloatCurves ?? [];
        var additiveFloatCurves = additiveSequence.CompressedCurveData.FloatCurves ?? [];

        FFloatCurve[] floatCurves = [..baseFloatCurves, ..additiveFloatCurves];
        foreach (var curve in floatCurves)
        {
            exportSequence.Curves.Add(new ExportCurve
            {
                Name = curve.CurveName.Text,
                Keys = curve.FloatCurve.Keys.Select(x => new ExportCurveKey(x.Time, x.Value)).ToList()
            });
        }

        return exportSequence;
    }
    
    
    public void PoseAsset(UPoseAsset poseAsset, ExportPoseDataMeta meta)
    {
        /* Only tested when bAdditivePose = true */
        if (!poseAsset.bAdditivePose)
        {
            Log.Warning($"{poseAsset.Name}: bAdditivePose = false is unsupported");
            return;
        }

        var poseContainer = poseAsset.PoseContainer;
        var poses = poseContainer.Poses;
        if (poses is null || poses.Length == 0)
        {
            Log.Warning($"{poseAsset.Name}: has no poses");
            return;
        }

        var poseNames = poseContainer.GetPoseNames().ToArray();
        if (poseNames is null || poseNames.Length == 0)
        {
            Log.Warning($"{poseAsset.Name}: PoseFNames is null or empty");
            return;
        }

        /* Assert number of tracks == number of bones for given skeleton */
        var poseTracks = poseContainer.Tracks;

        /* Propagate CurveTrackNames for CurveData processing. */
        if (poseContainer.Curves is not null)
            meta.CurveTrackNames = poseContainer.Curves.Select(x => x.CurveName.PlainText).ToArray();

        if (poseContainer.TrackPoseInfluenceIndices is not null)
        {
            var poseTrackInfluences = poseContainer.TrackPoseInfluenceIndices;
            if (poseTracks.Length != poseTrackInfluences.Length)
            {
                Log.Warning($"{poseAsset.Name}: length of Tracks != length of TrackPoseInfluenceIndices");
                return;
            }

            /* Add poses by name first in order they appear */
            for (var i = 0; i < poses.Length; i++)
            {
                var pose = poses[i];
                var poseName = poseNames[i];
                var poseData = new PoseData(poseName, pose.CurveData);
                meta.PoseData.Add(poseData);
            }

            /* Discover connection between bone name and relative location. */
            for (var i = 0; i < poseTrackInfluences.Length; i++)
            {
                var poseTrackInfluence = poseTrackInfluences[i];
                if (poseTrackInfluence is null) continue;

                var poseTrackName = poseTracks[i];
                foreach (var influence in poseTrackInfluence.Influences)
                {
                    var pose = meta.PoseData[influence.PoseIndex];
                    var transform = poses[influence.PoseIndex].LocalSpacePose[influence.BoneTransformIndex];
                    if (!transform.Rotation.IsNormalized)
                        transform.Rotation.Normalize();

                    pose.Keys.Add(new PoseKey(
                        poseTrackName.PlainText, /* Bone name to move */
                        transform.Translation,
                        transform.Rotation,
                        transform.Scale3D,
                        influence.PoseIndex,
                        influence.BoneTransformIndex
                    ));
                }
            }
        }
        else
        {
            /* Add poses by name first in order they appear */
            for (var i = 0; i < poses.Length; i++)
            {
                var pose = poses[i];
                var poseName = poseNames[i];
                var poseData = new PoseData(poseName, pose.CurveData);
                meta.PoseData.Add(poseData);
            }

            /* Discover connection between bone name and relative location. */
            for (var i = 0; i < poses.Length; i++)
            {
                var poseData = poses[i];

                foreach (var (trackIndex, bufferIndex) in poseData.TrackToBufferIndex)
                {
                    var transform = poseData.LocalSpacePose[bufferIndex];
                    if (!transform.Rotation.IsNormalized)
                        transform.Rotation.Normalize();

                    meta.PoseData[i].Keys.Add(new PoseKey(
                        poseTracks[trackIndex].PlainText, /* Bone name to move */
                        transform.Translation,
                        transform.Rotation,
                        transform.Scale3D,
                        trackIndex,
                        bufferIndex
                    ));
                }
            }
        }
    }
}