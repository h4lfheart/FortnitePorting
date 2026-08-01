using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Rendering;
using FortnitePorting.Models.Viewers;
using FortnitePorting.Rendering;
using FortnitePorting.Rendering.Actors;
using FortnitePorting.Rendering.Animation.Montage;
using FortnitePorting.Rendering.Components.Mesh;
using FortnitePorting.Rendering.Core;
using FortnitePorting.Rendering.Systems;
using FortnitePorting.Services;
using FortnitePorting.Windows;
using Material.Icons;
using OpenTK.Mathematics;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class ModelPreviewWindowModel(SettingsService settings, CUE4ParseService ueParse) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    private readonly CUE4ParseService _ueParse = ueParse;

    [ObservableProperty] private RenderingXControl? _control;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _loadCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _loadName = string.Empty;
    [ObservableProperty] private bool _isLoadingWorld;

    [ObservableProperty] private static RenderingXContext? _context;
    [ObservableProperty] private static Scene _scene = null!;
    [ObservableProperty] private static Actor _previewRoot = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ObjectDisplayName), nameof(ObjectTypeName), nameof(ObjectIcon))]
    private UObject? _primaryObject;

    public string ObjectDisplayName => PrimaryObject?.Name ?? "No Object";
    public string ObjectTypeName => PrimaryObject?.ExportType ?? string.Empty;
    public Bitmap? ObjectIcon => PrimaryObject?.GetEditorIconBitmap();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnimationDisplayName), nameof(AnimationTypeName), nameof(AnimationIcon))]
    private UAnimationAsset? _currentAnimation;

    public string AnimationDisplayName => CurrentAnimation?.Name ?? string.Empty;
    public string AnimationTypeName => CurrentAnimation?.ExportType ?? string.Empty;
    public Bitmap? AnimationIcon => CurrentAnimation?.GetEditorIconBitmap();

    [ObservableProperty] private bool _hasPreviewContent;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))]
    private bool _isPlaying;

    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;

    [ObservableProperty] private float _time;
    [ObservableProperty] private float _duration = 1f;

    [ObservableProperty] private ObservableCollection<AnimSectionItem> _sections = [];
    [ObservableProperty] private AnimSectionItem? _selectedSection;

    [ObservableProperty] private bool _hasSkeletalMesh;
    [ObservableProperty] private bool _hasAnimation;

    private MeshActor? _primarySkeletalActor;
    private bool _isScrubbing;
    private bool _wasPlayingBeforeScrub;
    private bool _syncingUi;

    private readonly DispatcherTimer _animationUiTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(33)
    };

    public void InitializeContext()
    {
        if (Context is null)
        {
            Scene = new Scene();
            Context = new RenderingXContext(Scene);

            var root = new Actor("Root");
            Scene.AddActor(root);

            var cameraActor = new CameraActor("MainCamera");
            cameraActor.Camera.Transform.Position = new Vector3(5, 5, 5);
            cameraActor.Camera.LookAt(Vector3.Zero);
            root.Children.Add(cameraActor);

            Scene.ActiveCamera = cameraActor.Camera;

            var grid = new Actor("Grid");
            grid.Components.Add(new GridMeshComponent());
            root.Children.Add(grid);

            PreviewRoot = new Actor("PreviewRoot");
            root.Children.Add(PreviewRoot);
        }

        Control = new RenderingXControl(Context);

        _animationUiTimer.Tick -= OnAnimationUiTick;
        _animationUiTimer.Tick += OnAnimationUiTick;
        _animationUiTimer.Start();
    }

    public override async Task OnViewExited()
    {
        _animationUiTimer.Stop();
        _animationUiTimer.Tick -= OnAnimationUiTick;
        await base.OnViewExited();
    }

    public void LoadScene(IEnumerable<UObject> objects, UAnimationAsset? animation = null)
    {
        Context?.EnqueueCommand(() =>
        {
            IsLoading = true;

            foreach (var existingChild in PreviewRoot.Children.ToArray())
                PreviewRoot.Children.Remove(existingChild);

            _primarySkeletalActor = null;
            UObject? primarySkeletalObject = null;
            UObject? firstPreviewObject = null;
            var placementOffset = Vector3.Zero;

            foreach (var obj in objects)
            {
                switch (obj)
                {
                    case UStaticMesh staticMesh:
                    {
                        firstPreviewObject ??= staticMesh;
                        var actor = new MeshActor(staticMesh, new Transform { Position = placementOffset });
                        var boundingBoxSize = actor.MeshComponent.Renderer.BoundingBox.GetSize();
                        placementOffset.X += boundingBoxSize.X * 0.01f + 1;
                        PreviewRoot.Children.Add(actor);
                        break;
                    }
                    case USkeletalMesh skeletalMesh:
                    {
                        firstPreviewObject ??= skeletalMesh;
                        primarySkeletalObject ??= skeletalMesh;
                        var actor = new MeshActor(skeletalMesh, new Transform { Position = placementOffset },
                            animation);
                        var boundingBoxSize = actor.MeshComponent.Renderer.BoundingBox.GetSize();
                        placementOffset.X += boundingBoxSize.X * 0.01f + 1;
                        PreviewRoot.Children.Add(actor);
                        _primarySkeletalActor ??= actor;
                        break;
                    }
                    case ULevel level:
                    {
                        firstPreviewObject ??= level;
                        IsLoadingWorld = true;
                        var worldActor = new WorldActor(level, progressHandler: progress =>
                        {
                            LoadCount = progress.Current;
                            TotalCount = progress.Total;
                            LoadName = progress.Name;
                        });
                        PreviewRoot.Children.Add(worldActor);
                        IsLoadingWorld = false;
                        break;
                    }
                }
            }

            var meshSystem = Scene.ActorManager.GetSystem<MeshRenderSystem>();
            Scene.ActiveCamera?.FrameBounds(meshSystem?.GetBounds() ?? new FBox());

            IsLoading = false;

            var primaryObject = primarySkeletalObject ?? firstPreviewObject;
            var hasPreviewContent = firstPreviewObject is not null;

            TaskService.RunDispatcher(() =>
            {
                PrimaryObject = primaryObject;
                HasPreviewContent = hasPreviewContent;
                CurrentAnimation = animation;
                SyncFromPose(forceSectionRefresh: true);
            });
        });
    }

    private void OnAnimationUiTick(object? sender, EventArgs e)
    {
        if (_isScrubbing) return;
        SyncFromPose();
    }

    private void SyncFromPose(bool forceSectionRefresh = false)
    {
        _syncingUi = true;
        try
        {
            var actor = _primarySkeletalActor;
            if (actor is null)
            {
                HasSkeletalMesh = false;
                HasAnimation = false;
                IsPlaying = false;
                Time = 0f;
                Duration = 1f;
                SelectedSection = null;
                Sections = [];
                return;
            }

            HasSkeletalMesh = true;
            HasAnimation = actor.HasAnimation;
            IsPlaying = actor.IsPlaying;
            Time = actor.Time;
            Duration = Math.Max(actor.Duration, 0.001f);

            RefreshSections(actor, forceSectionRefresh);
        }
        finally
        {
            _syncingUi = false;
        }
    }

    private void RefreshSections(MeshActor actor, bool force)
    {
        var sectionItems = actor.Sections.Select(CreateSectionItem).ToList();
        var namesChanged = force
                           || Sections.Count != sectionItems.Count
                           || !Sections.Select(s => s.Name).SequenceEqual(sectionItems.Select(s => s.Name));

        if (namesChanged)
        {
            SelectedSection = null;
            Sections = new ObservableCollection<AnimSectionItem>(sectionItems);
        }

        var sectionName = actor.CurrentSectionName;
        SelectedSection = Sections.FirstOrDefault(section =>
            string.Equals(section.Name, sectionName, StringComparison.OrdinalIgnoreCase));
    }

    private static AnimSectionItem CreateSectionItem(AnimMontageSection section)
    {
        var span = Math.Max(section.AnimEndTime - section.AnimStartTime, 0f);
        var rate = Math.Abs(section.PlayRate) < 1e-6f ? 1f : Math.Abs(section.PlayRate);
        var duration = span / rate;

        return new AnimSectionItem(section.Name, $"{duration:0.00}s");
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        var actor = _primarySkeletalActor;
        if (actor is null || !actor.HasAnimation) return;

        Context?.EnqueueCommand(() =>
        {
            if (actor.IsPlaying)
                actor.Pause();
            else
                actor.Resume();

            TaskService.RunDispatcher(() => SyncFromPose());
        });
    }

    [RelayCommand]
    public void ClearAnimation()
    {
        var actor = _primarySkeletalActor;
        if (actor is null) return;

        Context?.EnqueueCommand(() =>
        {
            actor.Stop();
            TaskService.RunDispatcher(() =>
            {
                CurrentAnimation = null;
                SelectedSection = null;
                Sections = [];
                SyncFromPose(forceSectionRefresh: true);
            });
        });
    }

    [RelayCommand]
    public async Task SelectAnimationAsync()
    {
        if (_primarySkeletalActor is null) return;

        if (await FilePickerWindow.OpenBrowserAsync("Select Animation") is not { Length: > 0 } paths)
            return;

        var animPath = Exporter.FixPath(paths[0]);
        var animation = await _ueParse.Provider.SafeLoadPackageObjectAsync<UAnimationAsset>(animPath);
        animation ??= await _ueParse.Provider.SafeLoadPackageObjectAsync(animPath) as UAnimationAsset;
        if (animation is null)
        {
            Info.Message("Model Viewer", "Selected file is not a valid animation asset.",
                severity: InfoBarSeverity.Warning);
            return;
        }

        ApplyAnimation(animation);
    }

    public void ApplyAnimation(UAnimationAsset animation)
    {
        var actor = _primarySkeletalActor;
        if (actor is null) return;

        Context?.EnqueueCommand(() =>
        {
            actor.Play(animation);
            TaskService.RunDispatcher(() =>
            {
                CurrentAnimation = animation;
                SyncFromPose(forceSectionRefresh: true);
            });
        });
    }

    public void JumpToSection(string? sectionName)
    {
        if (_syncingUi) return;

        var actor = _primarySkeletalActor;
        if (actor is null || string.IsNullOrWhiteSpace(sectionName)) return;

        Context?.EnqueueCommand(() =>
        {
            actor.JumpToSection(sectionName);
            TaskService.RunDispatcher(() => SyncFromPose());
        });
    }

    public void BeginScrub()
    {
        if (_isScrubbing) return;
        _isScrubbing = true;

        var actor = _primarySkeletalActor;
        if (actor is null || !actor.HasAnimation) return;

        _wasPlayingBeforeScrub = actor.IsPlaying;
        Context?.EnqueueCommand(() =>
        {
            actor.Pause();
            TaskService.RunDispatcher(() => SyncFromPose());
        });
    }

    public void EndScrub()
    {
        if (!_isScrubbing) return;
        _isScrubbing = false;

        var actor = _primarySkeletalActor;
        var shouldResume = _wasPlayingBeforeScrub;
        _wasPlayingBeforeScrub = false;

        if (actor is null || !actor.HasAnimation || !shouldResume)
        {
            SyncFromPose();
            return;
        }

        Context?.EnqueueCommand(() =>
        {
            actor.Resume();
            TaskService.RunDispatcher(() => SyncFromPose());
        });
    }

    public void ScrubTo(float time)
    {
        if (_syncingUi || !_isScrubbing) return;

        var actor = _primarySkeletalActor;
        if (actor is null || !actor.HasAnimation) return;

        Time = time;
        Context?.EnqueueCommand(() => actor.Seek(time));
    }
}