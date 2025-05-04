using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Animation.CurveExpression;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Types;

public class AnimExport : BaseExport
{
    public ExportMesh? Skeleton;
    public readonly List<ExportAnimSection> Sections = new();
    public readonly List<ExportSound> Sounds = new();
    public readonly List<ExportProp> Props = new();
    public List<ExportCurveMapping> LegacyToMetahumanMappings = [];
    public List<ExportCurveMapping> MetahumanToLegacyMappings = [];
    
    public AnimExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
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

        if (UEParse.Provider.TryLoadPackageObject<UCurveExpressionsDataAsset>(
                "FortniteGame/Content/Characters/Player/Common/Fortnite_Base_Head/Facials/CurveMappings/FN_LegacyTo3L_Main_Mapping",
                out var legacyToMetahumanCurves))
        {
            LegacyToMetahumanMappings = CurveMappings(legacyToMetahumanCurves);
        }
        
        if (UEParse.Provider.TryLoadPackageObject<UCurveExpressionsDataAsset>(
                "FortniteGame/Content/Characters/Player/Common/Fortnite_Base_Head/Facials/CurveMappings/FN_3LToLegacy_Main_Mapping",
                out var metahumanToLegacyCurves))
        {
            MetahumanToLegacyMappings = CurveMappings(metahumanToLegacyCurves);
        }
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
        var baseSequence = currentSection.LinkedSequence.Load<UAnimSequence>();
        if (baseSequence is null) return;

        ExportAnimSection? anim = null;
        if (montageRef.SlotAnimTracks.FirstOrDefault(slot => slot.SlotName.Text.Equals("AdditiveCorrective")) is
            { } additiveSlot)
        {
            var additiveSection = additiveSlot.AnimTrack.AnimSegments.FirstOrDefault(x => Math.Abs(x.StartPos - currentSection.SegmentBeginTime) < 0.01);
            var additiveSequence = additiveSection?.AnimReference.Load<UAnimSequence>();
            anim = Exporter.AnimSequence(additiveSequence, baseSequence);
        }
        
        anim ??= Exporter.AnimSequence(baseSequence);
        
        if (anim is not null)
        {
            anim.Name = currentSection.SectionName.Text;
            anim.Time = currentSection.SegmentBeginTime + time;
            anim.LinkValue = currentSection.LinkValue;
            anim.Loop = currentSection.SectionName == currentSection.NextSectionName || currentSection.NextSectionName.IsNone;
            anim.AssetRef = baseSequence;
            
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
                var sounds = new List<Sound>();
                sounds.AddRangeIfNotNull(soundNotify.EmoteSound1P?.HandleSoundTree(notify.TriggerTimeOffset));
                sounds.AddRangeIfNotNull(HandleMetaSound(soundNotify.MetaEmoteSound1P, notify.TriggerTimeOffset));
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
                
                if (propNotify.SkeletalMeshPropMontage is { } montage) HandleSectionTree(animSections, montage, montage.CompositeSections.First());
                if (animSections.Count == 0 && propNotify.SkeletalMeshPropAnimationMontage is { } secondMontage)
                {
                    var propExport = new AnimExport(secondMontage.Name, secondMontage, [], EExportType.Animation,
                        Exporter.Meta);

                    animSections = propExport.Sections;
                }
                
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

    private List<ExportCurveMapping> CurveMappings(UCurveExpressionsDataAsset curveExpressions)
    {
        var mappings = new List<ExportCurveMapping>();

        foreach (var (curveName, expr) in curveExpressions.ExpressionData.ExpressionMap)
        {
            var expressionStack = expr.Expression.Select(element => element switch
                {
                    OpElement<EOperator> op => new ExportCurveExpressionElement(OpElement.EOperator, op.Value),
                    OpElement<FName> name => new ExportCurveExpressionElement(OpElement.FName, name.Value.Text),
                    OpElement<FFunctionRef> functionRef => new ExportCurveExpressionElement(OpElement.FFunctionRef, functionRef.Value.Index),
                    OpElement<float> single => new ExportCurveExpressionElement(OpElement.Float, single.Value),
                })
                .ToList();

            mappings.Add(new ExportCurveMapping
            {
                Name = curveName.Text,
                ExpressionStack = expressionStack
            });
        }
        
        return mappings;
    }

    private List<Sound> HandleMetaSound(UMetaSoundSource? metaSoundSource, float offsetTime = 0.0f)
    {
        if (metaSoundSource is null) return [];
        
        var rootMetasoundDocument = metaSoundSource.GetOrDefault<FStructFallback?>("RootMetaSoundDocument") 
                                    ?? metaSoundSource.GetOrDefault<FStructFallback?>("RootMetasoundDocument");
        if (rootMetasoundDocument is null) return [];

        var sounds = new List<Sound>();
        var rootGraph = rootMetasoundDocument.Get<FStructFallback>("RootGraph");
        var interFace = rootGraph.Get<FStructFallback>("Interface");
        var inputs = interFace.Get<FStructFallback[]>("Inputs");
        foreach (var input in inputs)
        {
            var typeName = input.Get<FName>("TypeName");
            if (!typeName.Text.Contains("WaveAsset")) continue;
                
            var name = input.Get<FName>("Name");
            if (!name.Text.Contains("Loop")) continue;
            
            var literal = input.GetOrDefault<FStructFallback?>("DefaultLiteral");
            if (literal is null && input.TryGetValue(out FStructFallback[] defaults, "Defaults"))
            {
                literal = defaults.FirstOrDefault()?.GetOrDefault<FStructFallback?>("Literal");
            }

            var soundWave = literal?.Get<FPackageIndex[]>("AsUObject").FirstOrDefault();
            if (soundWave is null) continue;
            
            sounds.Add(SoundExtensions.CreateSound(soundWave));
        }

        return sounds;
    }
    
}