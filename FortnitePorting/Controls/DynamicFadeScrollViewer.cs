using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;

namespace FortnitePorting.Controls;

public class DynamicFadeScrollViewer : TemplatedControl
{
    public static readonly StyledProperty<double> FadeThresholdProperty =
        AvaloniaProperty.Register<DynamicFadeScrollViewer, double>(nameof(FadeThreshold), 25.0);

    public static readonly StyledProperty<double> FadePercentageProperty =
        AvaloniaProperty.Register<DynamicFadeScrollViewer, double>(nameof(FadePercentage), 0.05);

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<DynamicFadeScrollViewer, object?>(nameof(Content));

    public static readonly StyledProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
        ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner<DynamicFadeScrollViewer>();

    public static readonly StyledProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
        ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner<DynamicFadeScrollViewer>();

    public static readonly RoutedEvent<ScrollChangedEventArgs> ScrollChangedEvent =
        RoutedEvent.Register<DynamicFadeScrollViewer, ScrollChangedEventArgs>(
            nameof(ScrollChanged),
            RoutingStrategies.Bubble);

    public double FadeThreshold
    {
        get => GetValue(FadeThresholdProperty);
        set => SetValue(FadeThresholdProperty, value);
    }

    public double FadePercentage
    {
        get => GetValue(FadePercentageProperty);
        set => SetValue(FadePercentageProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    public event EventHandler<ScrollChangedEventArgs>? ScrollChanged
    {
        add => AddHandler(ScrollChangedEvent, value);
        remove => RemoveHandler(ScrollChangedEvent, value);
    }

    public ScrollViewer? ScrollViewer;

    private bool _showTopFade;
    private bool _showBottomFade;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (ScrollViewer != null)
        {
            ScrollViewer.ScrollChanged -= OnScrollChanged;
        }

        ScrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

        if (ScrollViewer != null)
        {
            ScrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (ScrollViewer == null) return;

        var distanceFromTop = ScrollViewer.Offset.Y;
        var distanceFromBottom = ScrollViewer.Extent.Height - ScrollViewer.Viewport.Height - ScrollViewer.Offset.Y;

        var shouldShowTopFade = distanceFromTop > FadeThreshold;
        var shouldShowBottomFade = distanceFromBottom > FadeThreshold;

        if (shouldShowTopFade != _showTopFade || shouldShowBottomFade != _showBottomFade)
        {
            _showTopFade = shouldShowTopFade;
            _showBottomFade = shouldShowBottomFade;
            UpdateOpacityMask();
        }

        var newArgs = new ScrollChangedEventArgs(
            e.OffsetDelta,
            e.ExtentDelta,
            e.ViewportDelta);
        
        newArgs.RoutedEvent = ScrollChangedEvent;
        newArgs.Source = this;
        
        RaiseEvent(newArgs);
    }

    private void UpdateOpacityMask()
    {
        if (ScrollViewer == null) return;

        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
        };

        if (_showTopFade)
        {
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), FadePercentage));
        }
        else
        {
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0));
        }

        if (_showBottomFade)
        {
            gradientBrush.GradientStops.Add(
                new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 1 - FadePercentage));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1));
        }
        else
        {
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 1));
        }

        ScrollViewer.OpacityMask = gradientBrush;
    }

    public void ScrollToEnd()
    {
        ScrollViewer?.ScrollToEnd();
    }

    public Vector Offset
    {
        get => ScrollViewer?.Offset ?? default;
        set
        {
            if (ScrollViewer != null)
            {
                ScrollViewer.Offset = value;
            }
        }
    }
    
    public Size Extent => ScrollViewer?.Extent ?? default;
    public Size Viewport => ScrollViewer?.Viewport ?? default;
}