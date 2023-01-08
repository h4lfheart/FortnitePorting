using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Exports.Types;

public class DanceExportData : ExportDataBase
{
    public AnimationData AnimData = new();

    public static async Task<DanceExportData> Create(UObject asset)
    {
        var data = new DanceExportData();
        data.Name = asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        data.Type = EAssetType.Dance.ToString();
        data.AnimData = await CreateAnimDataAsync(asset.Get<UAnimMontage>("Animation"));

        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }

    public static async Task<AnimationData> CreateAnimDataAsync(UAnimMontage montage)
    {
        var animData = new AnimationData();
        await Task.Run(() =>
        {
            var masterSkeleton = montage.Get<USkeleton>("Skeleton");
            ExportHelpers.Save(masterSkeleton);
            animData.Skeleton = masterSkeleton.GetPathName();

            var animation = GetExportSequence(montage);
            ExportHelpers.Save(animation);
            animData.Animation = animation.GetPathName();

            var montageNotifies = montage.GetOrDefault("Notifies", Array.Empty<FStructFallback>());
            var animNotifies = animation.GetOrDefault("Notifies", Array.Empty<FStructFallback>());

            var allNotifies = new List<FStructFallback>();
            allNotifies.AddRange(montageNotifies);
            allNotifies.AddRange(animNotifies);
            var propNotifies = allNotifies.Where(x =>
            {
                var notifyName = x.GetOrDefault<FName>("NotifyName").Text;
                return notifyName.Contains("FortSpawnProp") || notifyName.Contains("Fort Anim Notify State Spawn Prop");
            });

            foreach (var propNotify in propNotifies)
            {
                /*var linkedSequence = propNotify.Get<UAnimSequence>("LinkedSequence");
                if (linkedSequence != animation) continue;*/
                var notifyData = propNotify.Get<FortAnimNotifyState_SpawnProp>("NotifyStateClass");
                var exportProp = new EmotePropData
                {
                    SocketName = notifyData.SocketName.Text,
                    LocationOffset = notifyData.LocationOffset,
                    RotationOffset = notifyData.RotationOffset,
                    Scale = notifyData.Scale,
                    Prop = ExportHelpers.Mesh(notifyData.StaticMeshProp) ?? ExportHelpers.Mesh(notifyData.SkeletalMeshProp)
                };

                var propAnimation = notifyData.SkeletalMeshPropAnimation;
                propAnimation ??= GetExportSequence(notifyData.SkeletalMeshPropMontage);
                if (propAnimation is not null)
                {
                    ExportHelpers.Save(propAnimation);
                    exportProp.Animation = propAnimation.GetPathName();
                }

                animData.Props.Add(exportProp);
            }
        });

        return animData;
    }


    public static UAnimSequence? GetExportSequence(UAnimMontage? montage)
    {
        var sections = montage?.Get<FStructFallback[]>("CompositeSections");
        var targetSection = sections?.FirstOrDefault(x =>
        {
            var sectionText = x.GetOrDefault<FName>("SectionName").Text;
            return sectionText.Equals("Loop", StringComparison.OrdinalIgnoreCase);
        });
        targetSection ??= sections?.FirstOrDefault(x =>
        {
            var sectionText = x.GetOrDefault<FName>("SectionName").Text;
            return sectionText.Equals("Default", StringComparison.OrdinalIgnoreCase);
        });
        targetSection ??= sections?.FirstOrDefault(x =>
        {
            var sectionText = x.GetOrDefault<FName>("SectionName").Text;
            return sectionText.Equals("Success", StringComparison.OrdinalIgnoreCase);
        });

        targetSection ??= sections?.Last(); // TODO ADD USER PROMPT FOR SECTION

        var animation = targetSection?.Get<UAnimSequence>("LinkedSequence");
        return animation;
    }
}