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
    public string Animation;
    public string Skeleton;
    public List<EmotePropData> Props = new();
    public static async Task<DanceExportData> Create(UObject asset)
    {
        var data = new DanceExportData();
        data.Name = asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        data.Type = EAssetType.Dance.ToString();
        await Task.Run(() =>
        {
            var montage = asset.Get<UAnimMontage>("Animation");

            var masterSkeleton = montage.Get<USkeleton>("Skeleton");
            ExportHelpers.Save(masterSkeleton);
            data.Skeleton = masterSkeleton.GetPathName();

            var sections = montage.Get<FStructFallback[]>("CompositeSections");
            var targetSection = sections.FirstOrDefault(x =>
            {
                var sectionText = x.GetOrDefault<FName>("SectionName").Text;
                return sectionText.Equals("Loop");
            });
            targetSection ??= sections.FirstOrDefault(x =>
            {
                var sectionText = x.GetOrDefault<FName>("SectionName").Text;
                return sectionText.Equals("Default");
            });
            targetSection ??= sections.FirstOrDefault(x =>
            {
                var sectionText = x.GetOrDefault<FName>("SectionName").Text;
                return sectionText.Equals("Success");
            });

            targetSection ??= sections.Last(); // TODO ADD USER PROMPT FOR SECTION

            var animation = targetSection.Get<UAnimSequence>("LinkedSequence");
            ExportHelpers.Save(animation);
            data.Animation = animation.GetPathName();

            var notifies = montage.Get<FStructFallback[]>("Notifies");
            var propNotifies = notifies.Where(x =>
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
                if (propAnimation is not null)
                {
                    ExportHelpers.Save(propAnimation);
                    exportProp.Animation = propAnimation.GetPathName();
                }
                
                data.Props.Add(exportProp);
            }
        });

        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }
}

public class FortAnimNotifyState_SpawnProp : UObject
{
    public FName SocketName { get; private set; }
    public FVector LocationOffset { get; private set; }
    public FRotator RotationOffset { get; private set; }
    public FVector Scale { get; private set; }
    public bool bInheritScale { get; private set; }
    public UStaticMesh? StaticMeshProp { get; private set; }   
    public USkeletalMesh? SkeletalMeshProp { get; private set; }   
    public UAnimSequence? SkeletalMeshPropAnimation { get; private set; }
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SocketName = GetOrDefault<FName>(nameof(SocketName));
        LocationOffset = GetOrDefault<FVector>(nameof(LocationOffset), FVector.ZeroVector);
        RotationOffset = GetOrDefault<FRotator>(nameof(RotationOffset), FRotator.ZeroRotator);
        Scale = GetOrDefault<FVector>(nameof(Scale), FVector.OneVector);
        bInheritScale = GetOrDefault<bool>(nameof(bInheritScale));
        StaticMeshProp = GetOrDefault<UStaticMesh>(nameof(StaticMeshProp));
        SkeletalMeshProp = GetOrDefault<USkeletalMesh>(nameof(SkeletalMeshProp));
        SkeletalMeshPropAnimation = GetOrDefault<UAnimSequence>(nameof(SkeletalMeshPropAnimation));
    }
}
