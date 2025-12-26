using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using FortnitePorting.Services;

namespace FortnitePorting.Controls;

public partial class LevelUpOverlay : UserControl
{
    public LevelUpOverlay()
    {
        InitializeComponent();
    }

    public async Task ShowLevelUp(int level)
    {
        LevelText.Text = level.ToString();
        
        IsVisible = true;
        
        await Task.Delay(100);
        
        OverlayBackground.Opacity = 0;
        OverlayBackground.Classes.Remove("Show");
        BurstContent.Opacity = 0;
        BurstContent.RenderTransform = new ScaleTransform(0.7, 0.7);
        ParticleCanvas.Children.Clear();
        WaveCanvas.Children.Clear();
        
        await Task.Delay(50);
        
        OverlayBackground.Classes.Add("Show");
        
        AnimateContent();
        CreateEnergyWaves();
        CreateParticles();
        
        await Task.Delay(2500);
        
        await FadeOut();
        
        IsVisible = false;
        
        OverlayBackground.Classes.Remove("Show");
    }

    private async Task FadeOut()
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(0.3),
            Easing = new CubicEaseIn(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1.0),
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0.0),
                    }
                }
            }
        };

        await animation.RunAsync(OverlayBackground);
    }

    private async void AnimateContent()
    {
        AnimateScale();
        
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(0.6),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0.0),
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1.0),
                    }
                }
            }
        };

        await animation.RunAsync(BurstContent);
    }

    private void AnimateScale()
    {
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(0.6);
        
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        
        timer.Tick += (s, e) =>
        {
            var elapsed = DateTime.Now - startTime;
            var progress = Math.Min(elapsed.TotalSeconds / duration.TotalSeconds, 1.0);
            
            double scale;
            if (progress < 0.6)
            {
                var t = progress / 0.6;
                scale = 0.7 + (0.45 * EaseOutCubic(t));
            }
            else
            {
                var t = (progress - 0.6) / 0.4;
                scale = 1.15 - (0.15 * EaseOutCubic(t));
            }
            
            BurstContent.RenderTransform = new ScaleTransform(scale, scale);
            
            if (progress >= 1.0)
            {
                timer.Stop();
            }
        };
        
        timer.Start();
    }

    private double EaseOutCubic(double t)
    {
        return 1 - Math.Pow(1 - t, 3);
    }

    private void CreateEnergyWaves()
    {
        TaskService.RunDispatcher(() =>
        {
            var centerX = WaveCanvas.Bounds.Width / 2;
            var centerY = WaveCanvas.Bounds.Height / 2;
            
            for (var i = 0; i < 3; i++)
            {
                var wave = new Ellipse
                {
                    Width = 0,
                    Height = 0,
                    Opacity = 0
                };
                
                var gradient = new RadialGradientBrush
                {
                    GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
                    RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
                    GradientStops =
                    [
                        new GradientStop { Color = Color.Parse("#66953bf8"), Offset = 0 },
                        new GradientStop { Color = Color.Parse("#00953bf8"), Offset = 0.7 }
                    ]
                };
                
                wave.Fill = gradient;
                
                Canvas.SetLeft(wave, centerX);
                Canvas.SetTop(wave, centerY);
                
                WaveCanvas.Children.Add(wave);
                
                var delay = i * 300;
                AnimateWave(wave, delay, centerX, centerY);
            }
        }, DispatcherPriority.Render);
    }

    private async void AnimateWave(Ellipse wave, int delayMs, double centerX, double centerY)
    {
        await Task.Delay(delayMs);
        
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(1.5);
        
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        
        timer.Tick += (s, e) =>
        {
            var elapsed = DateTime.Now - startTime;
            var progress = Math.Min(elapsed.TotalSeconds / duration.TotalSeconds, 1.0);
            
            var eased = EaseOutCubic(progress);
            
            var size = eased * 800;
            wave.Width = size;
            wave.Height = size;
            
            Canvas.SetLeft(wave, centerX - size / 2);
            Canvas.SetTop(wave, centerY - size / 2);
            
            if (progress < 0.2)
            {
                wave.Opacity = (progress / 0.2) * 0.8;
            }
            else
            {
                wave.Opacity = 0.8 * (1 - ((progress - 0.2) / 0.8));
            }
            
            if (progress >= 1.0)
            {
                timer.Stop();
            }
        };
        
        timer.Start();
    }

    private void CreateParticles()
    {
        var random = new Random();
        
        TaskService.RunDispatcher(() =>
        {
            var centerX = ParticleCanvas.Bounds.Width / 2;
            var centerY = ParticleCanvas.Bounds.Height / 2;
            
            for (var i = 0; i < 30; i++)
            {
                var angle = (Math.PI * 2 * i) / 30;
                var distance = 150 + random.NextDouble() * 100;
                
                var particle = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = ParticleColor(i),
                    Opacity = 0
                };
                var gradient = new RadialGradientBrush
                {
                    GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
                    RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
                    GradientStops =
                    [
                        new GradientStop { Color = Color.Parse("#66953bf8"), Offset = 0.5 },
                        new GradientStop { Color = Color.Parse("#00953bf8"), Offset = 1 }
                    ]
                };
                
                particle.Fill = gradient;
                
                Canvas.SetLeft(particle, centerX - 4);
                Canvas.SetTop(particle, centerY - 4);
                
                ParticleCanvas.Children.Add(particle);
                
                var delay = random.Next(0, 200);
                AnimateParticle(particle, angle, distance, delay, centerX, centerY);
            }
        }, DispatcherPriority.Render);
    }

    private static SolidColorBrush ParticleColor(int index) => (index % 3) switch
    {
        0 => new SolidColorBrush(Color.Parse("#953bf8")),
        1 => new SolidColorBrush(Color.Parse("#7a2fd1")),
        _ => new SolidColorBrush(Color.Parse("#b565ff"))
    };

    private async void AnimateParticle(Ellipse particle, double angle, double distance, int delayMs, double centerX, double centerY)
    {
        await Task.Delay(delayMs);
        
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(1);
        
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        
        timer.Tick += (s, e) =>
        {
            var elapsed = DateTime.Now - startTime;
            var progress = Math.Min(elapsed.TotalSeconds / duration.TotalSeconds, 1.0);
            
            var eased = EaseOutCubic(progress);
            
            var currentX = Math.Cos(angle) * distance * eased;
            var currentY = Math.Sin(angle) * distance * eased;
            
            Canvas.SetLeft(particle, centerX + currentX - 4);
            Canvas.SetTop(particle, centerY + currentY - 4);
            
            if (progress < 0.2)
            {
                particle.Opacity = progress / 0.2;
            }
            else
            {
                particle.Opacity = 1 - ((progress - 0.2) / 0.8);
            }
            
            var scale = 1 - (eased * 0.5);
            particle.RenderTransform = new ScaleTransform(scale, scale);
            
            if (progress >= 1.0)
            {
                timer.Stop();
            }
        };
        
        timer.Start();
    }
}