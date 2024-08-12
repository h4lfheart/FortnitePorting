using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Settings;
using Serilog;

namespace FortnitePorting.Export.Types;

public class AnimExport : BaseExport
{
    public ExportObject? Skeleton;
    public readonly List<ExportAnimSection> Sections = new();
    public readonly List<ExportSound> Sounds = new();
    public readonly List<ExportProp> Props = new();
    
    public AnimExport(string name, UObject asset, FStructFallback[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        switch (exportType)
        {
            case EExportType.Animation:
            {
                switch (asset)
                {
                    case UAnimSequence animSequence:
                    {
                        if (animSequence.Skeleton.Load<USkeleton>() is { } skeleton)
                        {
                            Skeleton = Exporter.Skeleton(skeleton);
                        }
                        
                        Sections.AddIfNotNull(Exporter.AnimSequence(animSequence));
                        break;
                    }
                    case UAnimMontage animMontage:
                    {
                        AnimMontage(animMontage);
                        
                        break;
                    }
                }
                break;
            }
            case EExportType.Emote:
            {
                var montage = asset.GetOrDefault<UAnimMontage?>("Animation");
                montage ??= asset.GetOrDefault<UAnimMontage?>("FrontEndAnimation");
                if (montage is null) break;
                
                AnimMontage(montage);
                break;
            }
        }
    }
    
    public static AnimExport From(UAnimMontage montage, EExportType exportType, ExportDataMeta metaData)
    {
        return new AnimExport(montage.Name, montage, [], exportType, metaData);
    }
    
    private void AnimMontage(UAnimMontage montage)
    {
        Skeleton = Exporter.Skeleton(montage.Skeleton.Load<USkeleton>())!;
        HandleSectionTree(Sections, montage, montage.CompositeSections.First());

        var notifies = new List<FAnimNotifyEvent>();
        notifies.AddRange(montage.GetOrDefault("Notifies", Array.Empty<FAnimNotifyEvent>()));
        notifies.AddRange(Sections.SelectMany(section => section.AssetRef.Notifies));
        foreach (var notify in notifies)
        {
            HandleNotify(notify);
        }
    }

    private void HandleSectionTree(List<ExportAnimSection> sections, UAnimMontage montageRef, FCompositeSection currentSection, float time = 0.0f)
    {
        var sequence = currentSection.LinkedSequence.Load<UAnimSequence>();
        if (sequence is null) return;
        
        var anim = Exporter.AnimSequence(sequence);
        if (anim is not null)
        {
            anim.Name = currentSection.SectionName.Text;
            anim.Time = currentSection.SegmentBeginTime + time;
            anim.LinkValue = currentSection.LinkValue;
            anim.Loop = currentSection.SectionName == currentSection.NextSectionName || currentSection.NextSectionName.IsNone;
            anim.AssetRef = sequence;
            
            sections.Add(anim);
        
            if (anim.Loop) return;
        }

        var nextSection = montageRef.CompositeSections.FirstOrDefault(sec => currentSection.NextSectionName == sec.SectionName);
        if (nextSection is null) return;
        
        if (Sections.Any(section => section.Name.Equals(nextSection.SectionName.Text, StringComparison.OrdinalIgnoreCase))) return;

        var isSequentiallyNext = Math.Abs(nextSection.SegmentBeginTime - currentSection.SegmentBeginTime) < 0.01f;
        HandleSectionTree(sections, montageRef, nextSection, isSequentiallyNext ? time + currentSection.SegmentLength : time);
    }

    private void HandleNotify(FAnimNotifyEvent notify)
    {
        switch (notify.NotifyStateClass.Load())
        {
            case FortAnimNotifyState_EmoteSound soundNotify:
            {
                var sounds = soundNotify.EmoteSound1P?.HandleSoundTree(notify.TriggerTimeOffset);
                if (sounds is null) break;
                    
                foreach (var sound in sounds)
                {
                    Sounds.Add(new ExportSound
                    {
                        Path = Exporter.Export(sound.SoundWave.Load<USoundWave>()),
                        Time = sound.Time + notify.LinkValue ,
                        Loop = sound.Loop
                    });
                }
                break;
            }
            case FortAnimNotifyState_SpawnProp propNotify:
            {
                var mesh = Exporter.Mesh(propNotify.StaticMeshProp) ?? Exporter.Mesh(propNotify.SkeletalMeshProp);
                if (mesh is null) break;
                
                var animSections = new List<ExportAnimSection>();
                
                var montage = propNotify.SkeletalMeshPropMontage;
                if (montage is not null) HandleSectionTree(animSections, montage, montage.CompositeSections.First());
                if (animSections.Count == 0) animSections.AddIfNotNull(Exporter.AnimSequence(propNotify.SkeletalMeshPropAnimation));
                
                var prop = new ExportProp
                {
                    Mesh = mesh,
                    AnimSections = animSections,
                    SocketName = propNotify.SocketName.Text,
                    LocationOffset = propNotify.LocationOffset,
                    RotationOffset = propNotify.RotationOffset,
                    Scale = propNotify.Scale
                };

                Props.Add(prop);
                break;
            }
        }
    }
    
}