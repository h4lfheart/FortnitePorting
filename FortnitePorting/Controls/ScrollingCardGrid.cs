using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using FortnitePorting.Services;

namespace FortnitePorting.Controls;

public class ScrollingCardGrid : Canvas
{
    public static readonly StyledProperty<IEnumerable<string>> ImagePathsProperty =
        AvaloniaProperty.Register<ScrollingCardGrid, IEnumerable<string>>(nameof(ImagePaths), []);

    public IEnumerable<string> ImagePaths
    {
        get => GetValue(ImagePathsProperty);
        set => SetValue(ImagePathsProperty, value);
    }

    public static readonly StyledProperty<double> CardGapProperty =
        AvaloniaProperty.Register<ScrollingCardGrid, double>(nameof(CardGap), 16);

    public double CardGap
    {
        get => GetValue(CardGapProperty);
        set => SetValue(CardGapProperty, value);
    }

    private readonly List<ColumnInfo> _columns = [];
    private readonly DispatcherTimer _updateTimer;
    private readonly double[] _columnSpeeds = [0.5, 0.65, 0.55, 0.75, 0.6];
    
    private const int ColumnCount = 5;
    private const int CardsPerColumn = 12;

    public ScrollingCardGrid()
    {
        ClipToBounds = false;
        
        RenderTransform = new RotateTransform(3);
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        _updateTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1.0 / 30.0)
        };
        
        _updateTimer.Tick += UpdateCards;

        ImagePathsProperty.Changed.Subscribe(_ => TaskService.RunDispatcher(InitializeCardsAsync));
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        TaskService.RunDispatcher(InitializeCardsAsync);
        _updateTimer.Start();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _updateTimer.Stop();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) > 10 || 
            Math.Abs(e.PreviousSize.Height - e.NewSize.Height) > 10)
        {
            TaskService.RunDispatcher(InitializeCardsAsync);
        }
    }

    private void UpdateCards(object? sender, EventArgs e)
    {
        if (_columns.Count == 0) return;

        foreach (var column in _columns)
        {
            var cardHeight = column.Cards[0].Height;
            foreach (var card in column.Cards)
            {
                var currentTop = GetTop(card);
                var newTop = currentTop - column.Speed;

                if (newTop + cardHeight < -100)
                {
                    var maxBottom = column.Cards.Max(c => GetTop(c) + cardHeight);
                    newTop = maxBottom + CardGap;
                }
                
                SetTop(card, newTop);
            }
        }
    }

    private async Task InitializeCardsAsync()
    {
        if (!ImagePaths.Any()) return;
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        _columns.Clear();
        Children.Clear();

        var imageList = ImagePaths.ToList();
        var columnWidth = (Bounds.Width - (CardGap * (ColumnCount - 1))) / ColumnCount;

        for (var col = 0; col < ColumnCount; col++)
        {
            var column = new ColumnInfo
            {
                Index = col,
                Speed = _columnSpeeds[col % _columnSpeeds.Length],
                Cards = []
            };

            for (var i = 0; i < CardsPerColumn; i++)
            {
                var imgIndex = (col * 3 + i) % imageList.Count;
                var imagePath = imageList[imgIndex];

                var card = new CardControl
                {
                    Width = columnWidth,
                    Height = columnWidth * 9.0 / 16.0,
                    ImagePath = imagePath
                };

                column.Cards.Add(card);
                Children.Add(card);
            }

            _columns.Add(column);
        }

        await Task.Delay(10);
        PositionCards();
    }

    private void PositionCards()
    {
        if (_columns.Count == 0) return;

        var columnWidth = (Bounds.Width - (CardGap * (ColumnCount - 1))) / ColumnCount;

        foreach (var column in _columns)
        {
            var xPos = column.Index * (columnWidth + CardGap);
            var yOffset = 0.0;

            foreach (var card in column.Cards)
            {
                SetLeft(card, xPos);
                SetTop(card, yOffset);
                
                yOffset += card.Height + CardGap;
            }
        }
    }

    private class ColumnInfo
    {
        public int Index { get; init; }
        public double Speed { get; init; }
        public List<CardControl> Cards { get; init; } = [];
    }
}

public class CardControl : Border
{
    public static readonly StyledProperty<string> ImagePathProperty =
        AvaloniaProperty.Register<CardControl, string>(nameof(ImagePath));

    public string ImagePath
    {
        get => GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    private static readonly SolidColorBrush BackgroundBrush = SolidColorBrush.Parse("#18191b");
    private static readonly SolidColorBrush BorderColorBrush = new(Color.FromArgb(13, 255, 255, 255));

    public CardControl()
    {
        CornerRadius = new CornerRadius(12);
        ClipToBounds = true;
        
        Background = BackgroundBrush;
        BorderBrush = BorderColorBrush;
        BorderThickness = new Thickness(1);

        var image = new Image
        {
            Stretch = Stretch.UniformToFill,
            Opacity = 0.3
        };

        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.None);

        image.Bind(AsyncImageLoader.ImageLoader.SourceProperty, 
            new Avalonia.Data.Binding(nameof(ImagePath)) { Source = this });

        Child = image;
    }
}