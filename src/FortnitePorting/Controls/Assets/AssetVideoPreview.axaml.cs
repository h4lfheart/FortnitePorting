using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Services;
using LibVLCSharp.Shared;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Assets;

public partial class AssetVideoPreview : UserControl
{
    private static readonly Lazy<LibVLC> SharedLibVLC = new(CreateLibVLC);

    [AvaStyledProperty] private string _cosmeticId = string.Empty;
    [AvaDirectProperty] private bool _isVideoReady;

    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private CancellationTokenSource? _loadCts;
    private AssetVideoFrameSource? _frameSource;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    private FortniteGGPreviewResponse? _preview;

    public AssetVideoPreview()
    {
        InitializeComponent();

        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    private void OnFortniteGGPressed(object? sender, PointerPressedEventArgs e)
    {
        App.Launch(_preview?.CosmeticsUrl ?? "https://fortnite.gg");
        e.Handled = true;
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (!AppSettings.Application.ShowVideoPreviews) return;
        if (string.IsNullOrWhiteSpace(CosmeticId)) return;

        IsVideoReady = false;
        _preview = null;

        var cts = new CancellationTokenSource();
        _loadCts = cts;

        var cosmeticId = CosmeticId;
        TaskService.Run(async () => await LoadAsync(cosmeticId, cts.Token));
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        IsVideoReady = false;
        _preview = null;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        TaskService.Run(async () => await TearDownAsync());
    }

    private async Task LoadAsync(string cosmeticId, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycleLock.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (cancellationToken.IsCancellationRequested) return;

            var preview = await Api.FortniteGG.ResolvePreview(cosmeticId);
            if (cancellationToken.IsCancellationRequested || preview is null) return;

            await TaskService.RunDispatcherAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                _preview = preview;
                StartPlayback(preview.VideoUrl);
            });
        }
        catch (OperationCanceledException) { }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private async Task TearDownAsync()
    {
        await _lifecycleLock.WaitAsync();
        try
        {
            var player = _mediaPlayer;
            var media = _media;
            var frameSource = _frameSource;

            _mediaPlayer = null;
            _media = null;
            _frameSource = null;

            frameSource?.Active = false;

            if (player is not null)
            {
                player.EndReached -= OnEndReached;
                try
                {
                    player.Stop();
                    player.Dispose();
                    media?.Dispose();
                }
                catch
                {
                }
            }

            await TaskService.RunDispatcherAsync(() => frameSource?.Dispose());
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private void StartPlayback(string url)
    {
        _frameSource = new AssetVideoFrameSource(FrameImage, OnFramePresented);
        _frameSource.Active = true;

        _mediaPlayer = new MediaPlayer(SharedLibVLC.Value)
        {
            EnableHardwareDecoding = false,
            Mute = false,
            Volume = 100
        };
        _mediaPlayer.SetVideoFormatCallbacks(_frameSource.FormatCallback, _frameSource.CleanupCallback);
        _mediaPlayer.SetVideoCallbacks(_frameSource.LockCallback, null, _frameSource.DisplayCallback);
        _mediaPlayer.EndReached += OnEndReached;

        _media = new Media(SharedLibVLC.Value, new Uri(url),
            ":input-repeat=65535",
            ":network-caching=300");
        _mediaPlayer.Play(_media);
    }

    private void OnFramePresented()
    {
        if (!IsVideoReady)
            IsVideoReady = true;
    }

    private void OnEndReached(object? sender, EventArgs e)
    {
        TaskService.Run(() =>
        {
            if (_mediaPlayer is null) return;

            _mediaPlayer.Position = 0;
            _mediaPlayer.Play();
        });
    }

    private static LibVLC CreateLibVLC()
    {
        Core.Initialize();
        return new LibVLC(false,
            "--quiet",
            "--verbose=-1",
            "--no-video-title-show",
            "--no-video-on-top",
            "--avcodec-hw=none");
    }
}
