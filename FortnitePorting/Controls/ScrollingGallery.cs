using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace FortnitePorting.Controls;

public class ScrollingGallery : Canvas
{
    public static readonly StyledProperty<IEnumerable<string>> ImagePathsProperty =
        AvaloniaProperty.Register<ScrollingGallery, IEnumerable<string>>(nameof(ImagePaths));

    public IEnumerable<string> ImagePaths
    {
        get => GetValue(ImagePathsProperty);
        set => SetValue(ImagePathsProperty, value);
    }

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<ScrollingGallery, double>(nameof(Speed), 1.0);

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    private IEnumerable<string> _previousImagePaths;

    public ScrollingGallery()
    {
        var updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.0 / 60.0)
        };
        updateTimer.Tick += UpdateImages;
        updateTimer.Start();

        ImagePathsProperty.Changed.Subscribe(async _ => await InitializeImagesAsync());
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        await InitializeImagesAsync();
    }

    protected override async void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        await InitializeImagesAsync();
    }

    public async Task InitializeImagesAsync()
    {
        if (ImagePaths != _previousImagePaths)
        {
            _previousImagePaths = ImagePaths;

            Children.Clear(); 
            if (ImagePaths == null) return;

            double xOffset = 0;

            var imageTasks = ImagePaths.Select(async imagePath =>
            {
                var image = new Image
                {
                    Stretch = Stretch.UniformToFill
                };

                image.Bind(HeightProperty, new Binding("Bounds.Height")
                {
                    Source = this
                });

                image.SetValue(AsyncImageLoader.ImageLoader.SourceProperty, imagePath);

                var tcs = new TaskCompletionSource<object>();

                image.GetObservable(Image.SourceProperty).Subscribe(source =>
                {
                    if (source != null)
                    {
                        tcs.SetResult(null); 
                    }
                });

                await tcs.Task;

                return image;
            });

            var loadedImages = await Task.WhenAll(imageTasks);
            
            Dispatcher.UIThread.Post(() =>
            {
                double xOffset = 0;
                foreach (var image in loadedImages)
                {
                    if (image.Source is Bitmap bitmap)
                    {
                        var canvasHeight = Bounds.Height;
                        var imageWidth = bitmap.PixelSize.Width * (canvasHeight / bitmap.PixelSize.Height);

                        SetLeft(image, xOffset);
                        xOffset += imageWidth;

                        Children.Add(image);
                    }
                }
            });
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                double xOffset = 0;
                foreach (var image in Children.OfType<Image>())
                {
                    if (image.Source is Bitmap bitmap)
                    {
                        var canvasHeight = Bounds.Height;
                        var imageWidth = bitmap.PixelSize.Width * (canvasHeight / bitmap.PixelSize.Height);

                        SetLeft(image, xOffset);
                        xOffset += imageWidth;
                    }
                }
            });
        }
    }

    private void UpdateImages(object sender, EventArgs e)
    {
        foreach (var child in Children.OfType<Image>())
        {
            var currentLeft = GetLeft(child);
            SetLeft(child, currentLeft - Speed);

            if (Speed > 0)
            {
                if (currentLeft + child.Bounds.Width < 0)
                {
                    var maxRight = Children.OfType<Image>()
                        .Max(img => GetLeft(img) + img.Bounds.Width);
                    SetLeft(child, maxRight);
                }
            }
            else if (Speed < 0)
            {
                if (currentLeft > Bounds.Width)
                {
                    var minLeft = Children.OfType<Image>()
                        .Min(img => GetLeft(img));
                    SetLeft(child, minLeft - child.Bounds.Width);
                }
            }
        }
    }
}



