using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls;

public partial class CardCarousel : UserControl
{
    private const double ItemSpacing = 16;
    private static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(400);

    [AvaStyledProperty] private IEnumerable? _itemsSource;
    [AvaStyledProperty] private IDataTemplate? _itemTemplate;
    [AvaStyledProperty] private int _visibleCount = 3;
    [AvaStyledProperty] private bool _loop;
    [AvaStyledProperty] private TimeSpan _autoScrollInterval;
    [AvaStyledProperty] private int _autoScrollStep = 3;

    [AvaDirectProperty] private bool _canGoPrevious;
    [AvaDirectProperty] private bool _canGoNext;
    [AvaDirectProperty] private bool _hasItems;
    [AvaDirectProperty] private double _itemWidth;
    [AvaDirectProperty] private int _stripColumns = 1;
    [AvaDirectProperty] private double _stripWidth = double.NaN;

    public ObservableCollection<object?> DisplayItems { get; } = [];

    private int _offset;
    private int _sourceCount;
    private double _stepSize;
    private bool _isAnimating;
    private INotifyCollectionChanged? _trackedCollection;
    private CancellationTokenSource? _animationCts;
    private readonly DispatcherTimer _autoScrollTimer;

    private TranslateTransform StripTranslate => (TranslateTransform) StripItems.RenderTransform!;

    private int ResolvedVisibleCount => VisibleCount > 0 ? VisibleCount : 3;
    private int ResolvedAutoScrollStep => AutoScrollStep > 0 ? AutoScrollStep : 3;

    public CardCarousel()
    {
        _autoScrollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _autoScrollTimer.Tick += OnAutoScrollTick;

        InitializeComponent();

        if (VisibleCount <= 0) VisibleCount = 3;
        if (AutoScrollStep <= 0) AutoScrollStep = 3;

        this.GetObservable(ItemsSourceProperty).Subscribe(_ => OnItemsSourceChanged());
        this.GetObservable(VisibleCountProperty).Subscribe(_ => RebuildStrip());
        this.GetObservable(LoopProperty).Subscribe(_ => RebuildStrip());
        this.GetObservable(AutoScrollStepProperty).Subscribe(_ => RebuildStrip());
        this.GetObservable(AutoScrollIntervalProperty).Subscribe(_ => UpdateTimer());
        this.GetObservable(IsVisibleProperty).Subscribe(_ => UpdateTimer());

        AttachedToVisualTree += (_, _) => UpdateTimer();
        DetachedFromVisualTree += (_, _) =>
        {
            _animationCts?.Cancel();
            _autoScrollTimer.Stop();
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AttachCollectionChanged(ItemsSource);
        RebuildStrip();
        UpdateTimer();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _animationCts?.Cancel();
        DetachCollectionChanged();
        _autoScrollTimer.Stop();
    }

    [RelayCommand]
    private Task Previous() => MoveAsync(-1, resetTimer: true);

    [RelayCommand]
    private Task Next() => MoveAsync(1, resetTimer: true);

    private void OnAutoScrollTick(object? sender, EventArgs e)
    {
        if (_isAnimating || !ShouldAutoScroll()) return;
        _ = MoveAsync(ResolvedAutoScrollStep);
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateItemWidth();
        ApplyTranslate(animate: false);
    }

    private async Task MoveAsync(int delta, bool resetTimer = false)
    {
        var count = _sourceCount;
        if (count == 0 || delta == 0) return;

        if (!Loop)
        {
            var maxOffset = Math.Max(0, count - ResolvedVisibleCount);
            var target = Math.Clamp(_offset + delta, 0, maxOffset);
            if (target == _offset) return;
            _offset = target;
        }
        else
        {
            if (delta < 0 && _offset + delta < 0)
            {
                _offset += _sourceCount;
                ApplyTranslate(animate: false);
            }

            _offset += delta;
        }

        UpdateButtonState(count);
        if (resetTimer)
        {
            RestartTimer();
        }

        await AnimateTranslateAsync();
    }

    private void OnItemsSourceChanged()
    {
        AttachCollectionChanged(ItemsSource);
        RebuildStrip();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildStrip();
    }

    private void AttachCollectionChanged(IEnumerable? source)
    {
        DetachCollectionChanged();
        if (source is not INotifyCollectionChanged notify) return;

        notify.CollectionChanged += OnCollectionChanged;
        _trackedCollection = notify;
    }

    private void DetachCollectionChanged()
    {
        if (_trackedCollection is null) return;

        _trackedCollection.CollectionChanged -= OnCollectionChanged;
        _trackedCollection = null;
    }

    private void RebuildStrip()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RebuildStrip);
            return;
        }

        var items = GetItems();
        _sourceCount = items.Length;
        HasItems = _sourceCount > 0;

        DisplayItems.Clear();
        StripColumns = 1;
        StripWidth = double.NaN;

        if (_sourceCount == 0)
        {
            _offset = 0;
            UpdateButtonState(0);
            UpdateTimer();
            ApplyTranslate(animate: false);
            return;
        }

        if (Loop)
        {
            var needed = _sourceCount * 2 + Math.Max(ResolvedAutoScrollStep, 1) + ResolvedVisibleCount;
            var copies = Math.Max(3, (int) Math.Ceiling(needed / (double) _sourceCount));
            for (var copy = 0; copy < copies; copy++)
            {
                foreach (var item in items)
                {
                    DisplayItems.Add(item);
                }
            }
        }
        else
        {
            foreach (var item in items)
            {
                DisplayItems.Add(item);
            }
        }

        _offset = 0;
        StripColumns = Math.Max(1, DisplayItems.Count);

        UpdateItemWidth();
        UpdateButtonState(_sourceCount);
        UpdateTimer();
        ApplyTranslate(animate: false);
    }

    private void UpdateButtonState(int count)
    {
        if (Loop)
        {
            CanGoPrevious = true;
            CanGoNext = true;
            return;
        }

        var maxOffset = Math.Max(0, count - ResolvedVisibleCount);
        CanGoPrevious = _offset > 0;
        CanGoNext = _offset < maxOffset;
    }

    private void UpdateItemWidth()
    {
        var viewportWidth = ViewportBorder.Bounds.Width;
        if (viewportWidth <= 0 || ResolvedVisibleCount <= 0) return;

        ItemWidth = Math.Max(0, (viewportWidth - ItemSpacing * (ResolvedVisibleCount - 1)) / ResolvedVisibleCount);
        _stepSize = ItemWidth + ItemSpacing;

        var count = DisplayItems.Count;
        StripWidth = count > 0
            ? ItemWidth * count + ItemSpacing * Math.Max(0, count - 1)
            : double.NaN;
    }

    private async Task AnimateTranslateAsync()
    {
        _animationCts?.Cancel();
        _animationCts = new CancellationTokenSource();
        var token = _animationCts.Token;

        var from = StripTranslate.X;
        var to = GetTranslateX();
        if (Math.Abs(from - to) < 0.5)
        {
            StripTranslate.X = to;
            NormalizeLoopOffset();
            return;
        }

        _isAnimating = true;

        var animation = new Animation
        {
            Duration = SlideDuration,
            Easing = new SplineEasing(0.1, 0.9, 0.2, 1.0),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(TranslateTransform.XProperty, from) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(TranslateTransform.XProperty, to) }
                }
            }
        };

        try
        {
            await animation.RunAsync(StripItems, token);
            if (token.IsCancellationRequested) return;

            StripTranslate.X = to;
            NormalizeLoopOffset();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            StripTranslate.X = to;
            NormalizeLoopOffset();
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                _isAnimating = false;
            }
        }
    }

    private void ApplyTranslate(bool animate)
    {
        if (animate)
        {
            _ = AnimateTranslateAsync();
            return;
        }

        StripTranslate.X = GetTranslateX();
    }

    private double GetTranslateX() => _stepSize <= 0 ? 0 : -_offset * _stepSize;

    private void NormalizeLoopOffset()
    {
        if (!Loop || _sourceCount == 0) return;

        var original = _offset;
        while (_offset >= _sourceCount)
        {
            _offset -= _sourceCount;
        }

        while (_offset < 0)
        {
            _offset += _sourceCount;
        }

        if (_offset != original)
        {
            StripTranslate.X = GetTranslateX();
        }
    }

    private void UpdateTimer()
    {
        if (ShouldAutoScroll())
        {
            _autoScrollTimer.Interval = AutoScrollInterval;
            _autoScrollTimer.Start();
        }
        else
        {
            _autoScrollTimer.Stop();
        }
    }

    private void RestartTimer()
    {
        if (!ShouldAutoScroll()) return;

        _autoScrollTimer.Stop();
        _autoScrollTimer.Interval = AutoScrollInterval;
        _autoScrollTimer.Start();
    }

    private bool ShouldAutoScroll() =>
        AutoScrollInterval > TimeSpan.Zero && HasItems && IsLoaded && IsEffectivelyVisible;

    private object?[] GetItems()
    {
        return ItemsSource?.Cast<object?>().ToArray() ?? [];
    }
}
