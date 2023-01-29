using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using UObject = CUE4Parse.UE4.Assets.Exports.UObject;

namespace FortnitePorting.Exports.Types;

public class DanceExportData : ExportDataBase
{
    public AnimationData BaseAnimData = new();

    public static async Task<DanceExportData> Create(UObject asset)
    {
        var data = new DanceExportData();
        data.Name = asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        data.Type = EAssetType.Dance.ToString();

        var baseMontage = asset.GetOrDefault<UAnimMontage?>("Animation");
        baseMontage ??= asset.GetOrDefault<UAnimMontage?>("FrontEndAnimation");
        
        var additiveMontage = asset.GetOrDefault<UAnimMontage?>("AnimationFemaleOverride");
        additiveMontage ??= asset.GetOrDefault<UAnimMontage?>("FrontEndAnimationFemaleOverride");
        data.BaseAnimData = await CreateAnimDataAsync(baseMontage, additiveMontage);
        
       

        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }

    public static async Task<AnimationData> CreateAnimDataAsync(UAnimMontage? baseMontage, UAnimMontage? additiveMontage = null)
    {
        var animData = new AnimationData();
        if (baseMontage is null) return animData;

        await Task.Run(() =>
        {
            var masterSkeleton = baseMontage.Skeleton.Load<USkeleton>();
            if (masterSkeleton is null) return;
            
            ExportHelpers.Save(masterSkeleton);
            animData.Skeleton = masterSkeleton.GetPathName();
            
            // Sections
            if (additiveMontage is not null)
            {
                ExportSections(baseMontage, animData.Sections, additiveMontage);
            }
            else
            {
                ExportSections(baseMontage, animData.Sections);
            }

                // Notifies
            var montageNotifies = baseMontage.GetOrDefault("Notifies", Array.Empty<FStructFallback>());
            var propNotifies = new List<FStructFallback>();
            var soundNotifies = new List<FStructFallback>();
            foreach (var notify in montageNotifies)
            {
                var notifyName = notify.GetOrDefault<FName>("NotifyName").Text;
                if (notifyName.Contains("FortSpawnProp") || notifyName.Contains("Fort Anim Notify State Spawn Prop"))
                {
                    propNotifies.Add(notify);
                }
                if (notifyName.Contains("FortEmoteSound") || notifyName.Contains("Fort Anim Notify State Emote Sound"))
                {
                    soundNotifies.Add(notify);
                }
            }
            foreach (var propNotify in propNotifies)
            {
                var notifyData = propNotify.Get<FortAnimNotifyState_SpawnProp>("NotifyStateClass");
                var exportProp = new EmoteProp
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
            foreach (var soundNotify in soundNotifies)
            {
                var time = soundNotify.Get<float>("TriggerTimeOffset");
                
                void ExportEmoteSound(USoundNodeWavePlayer player, float timeOffset = 0)
                {
                    var soundWave = player.SoundWave?.Load<USoundWave>();
                    if (soundWave is null) return;
                    ExportHelpers.SaveSoundWave(soundWave, out var audioFormat);
                    animData.Sounds.Add(new EmoteSound(soundWave.GetPathName(), audioFormat, time+timeOffset, player.GetOrDefault("bLooping", false)));
                }

                void HandleAudioTree(UObject node, float offset = 0f)
                {
                    switch (node)
                    {
                        case USoundNodeWavePlayer player:
                        {
                            ExportEmoteSound(player, offset);
                            break;
                        }
                        case USoundNodeDelay delay:
                        {
                            foreach (var nodeObject in delay.ChildNodes)
                            {
                                HandleAudioTree(nodeObject.Load(), offset + delay.Get<float>("DelayMin")); // Max/Min are equal for emotes
                            }
                            break;
                        }
                        case USoundNodeRandom random:
                        {
                            var index = App.RandomGenerator.Next(0, random.ChildNodes.Length);
                            HandleAudioTree(random.ChildNodes[index].Load(), offset);
                            break;
                        }
                        
                        case UFortSoundNodeLicensedContentSwitcher switcher:
                        {
                            HandleAudioTree(switcher.ChildNodes.Last().Load(), offset);
                            break;
                        }
                        case USoundNode generic:
                        {
                            foreach (var nodeObject in generic.ChildNodes)
                            {
                                HandleAudioTree(nodeObject.Load(), offset);
                            }
                            break;
                        }
                    }
                }
                
                var notifyData = soundNotify.Get<FortAnimNotifyState_EmoteSound>("NotifyStateClass");
                var firstNode = notifyData.EmoteSound1P?.FirstNode?.Load();
                if (firstNode is null) continue;
                HandleAudioTree(firstNode);
            }
            
        });

        return animData;
    }

    private static void ExportSections(UAnimMontage targetMontage, List<EmoteSection> sections, UAnimMontage? additiveMontage = null)
    {
        var section = targetMontage.CompositeSections.FirstOrDefault();
        while (true)
        {
            if (section is null) break;
            if (section.LinkedSequence is not null) // empty sections are fine
            {
                var exportSection = new EmoteSection(section.LinkedSequence.GetPathName(), section.SectionName.Text, section.SegmentBeginTime, section.SegmentLength, section.NextSectionName == section.SectionName);
                ExportHelpers.Save(section.LinkedSequence);

                /*if (additiveMontage is not null)
                {
                    var additiveSlot = additiveMontage.SlotAnimTracks.First(x => x.SlotName.Text.Equals("AdditiveCorrective"));
                    var additiveSection = additiveSlot.AnimTrack.AnimSegments.First(x => Math.Abs(x.StartPos - section.SegmentBeginTime) < 0.01);

                    var additiveAnimation = additiveSection.AnimReference;
                    exportSection.AdditivePath = additiveAnimation.GetPathName();
                    ExportHelpers.SaveAdditiveAnim(section.LinkedSequence, additiveAnimation);
                }*/

                var floatCurves = section.LinkedSequence.CompressedCurveData.FloatCurves ?? Array.Empty<FFloatCurve>();
                foreach (var curve in floatCurves)
                {
                    exportSection.Curves.Add(new Curve
                    {
                        Name = curve.Name.DisplayName.Text,
                        Keys = curve.FloatCurve.Keys.Select(x => new CurveKey(x.Time, x.Value)).ToList()
                    });
                }

                sections.Add(exportSection);
            }
                
            // current section checks
            var currentSecitonName = section.SectionName.Text;
            var nextSectionName = section.NextSectionName.Text;
            if (currentSecitonName.Equals(nextSectionName) || nextSectionName.Equals("None")) break;
                
            // move onto next
            var nextSection = targetMontage.CompositeSections.FirstOrDefault(x => x.SectionName.Text.Equals(section.NextSectionName.Text));
            if (nextSection is null) break;
            if (Math.Abs(nextSection.SegmentBeginTime - section.SegmentBeginTime) < 0.01F) break;
            section = nextSection;
        }
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