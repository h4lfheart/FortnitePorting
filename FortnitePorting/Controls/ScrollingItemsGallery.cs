using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace FortnitePorting.Controls;

public class ScrollingItemsGallery : Canvas
{
    public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
        AvaloniaProperty.Register<ScrollingItemsGallery, IEnumerable>(nameof(ItemsSource));

    public IEnumerable ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        AvaloniaProperty.Register<ScrollingItemsGallery, IDataTemplate>(nameof(ItemTemplate));

    public IDataTemplate ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<ScrollingItemsGallery, double>(nameof(Speed), 1.0);

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<ScrollingItemsGallery, double>(nameof(ItemSpacing), 0.0);

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    private IEnumerable _previousItemsSource;
    private IDataTemplate _previousItemTemplate;

    public ScrollingItemsGallery()
    {
        var updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.0 / 60.0)
        };
        updateTimer.Tick += UpdateItems;
        updateTimer.Start();

        ItemsSourceProperty.Changed.Subscribe(async _ => await InitializeItemsAsync());
        ItemTemplateProperty.Changed.Subscribe(async _ => await InitializeItemsAsync());
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        await InitializeItemsAsync();
    }

    protected override async void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        await InitializeItemsAsync();
    }

    public async Task InitializeItemsAsync()
    {
        // Check if we need to recreate items
        bool needsRecreation = ItemsSource != _previousItemsSource || 
                              ItemTemplate != _previousItemTemplate;

        if (needsRecreation)
        {
            _previousItemsSource = ItemsSource;
            _previousItemTemplate = ItemTemplate;

            Children.Clear();
            
            if (ItemsSource == null || ItemTemplate == null) 
                return;

            var items = ItemsSource.Cast<object>().ToList();
            if (!items.Any()) 
                return;

            await Task.Run(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    double xOffset = 0;

                    foreach (var item in items)
                    {
                        var control = ItemTemplate.Build(item);
                        
                        if (control != null)
                        {
                            control.DataContext = item;
                            
                            control.Bind(HeightProperty, new Binding("Bounds.Height")
                            {
                                Source = this
                            });

                            SetLeft(control, xOffset);
                            Children.Add(control);

                            xOffset += 200 + ItemSpacing;
                        }
                    }

                    InvalidateMeasure();
                    InvalidateArrange();
                });
            });
        }
        else
        {
            await RepositionItemsAsync();
        }
    }

    private async Task RepositionItemsAsync()
    {
        await Task.Run(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                double xOffset = 0;
                
                foreach (var child in Children)
                {
                    SetLeft(child, xOffset);
                    xOffset += child.Bounds.Width + ItemSpacing;
                }
            });
        });
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var result = base.MeasureOverride(availableSize);
        
        Dispatcher.UIThread.Post(async () =>
        {
            await RepositionItemsWithCorrectWidths();
        });
        
        return result;
    }

    private async Task RepositionItemsWithCorrectWidths()
    {
        await Task.Run(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                double xOffset = 0;
                
                foreach (var child in Children)
                {
                    SetLeft(child, xOffset);
                    xOffset += Math.Max(child.Bounds.Width, child.DesiredSize.Width) + ItemSpacing;
                }
            });
        });
    }

    private void UpdateItems(object sender, EventArgs e)
    {
        if (!Children.Any()) return;

        foreach (var child in Children)
        {
            var currentLeft = GetLeft(child);
            SetLeft(child, currentLeft - Speed);

            var childWidth = Math.Max(child.Bounds.Width, child.DesiredSize.Width);

            if (Speed > 0)
            {
                // Moving left - when item goes off left edge, move to right
                if (currentLeft + childWidth < 0)
                {
                    var maxRight = Children
                        .Select(c => GetLeft(c) + Math.Max(c.Bounds.Width, c.DesiredSize.Width))
                        .DefaultIfEmpty(0)
                        .Max();
                    SetLeft(child, maxRight + ItemSpacing);
                }
            }
            else if (Speed < 0)
            {
                // Moving right - when item goes off right edge, move to left
                if (currentLeft > Bounds.Width)
                {
                    var minLeft = Children
                        .Select(c => GetLeft(c))
                        .DefaultIfEmpty(0)
                        .Min();
                    SetLeft(child, minLeft - childWidth - ItemSpacing);
                }
            }
        }
    }

    /// <summary>
    /// Stops the scrolling animation
    /// </summary>
    public void Stop()
    {
        Speed = 0;
    }

    /// <summary>
    /// Starts or resumes scrolling with the specified speed
    /// </summary>
    /// <param name="speed">Speed of scrolling (positive = left, negative = right)</param>
    public void Start(double speed = 1.0)
    {
        Speed = speed;
    }

    /// <summary>
    /// Reverses the scrolling direction
    /// </summary>
    public void Reverse()
    {
        Speed = -Speed;
    }
}