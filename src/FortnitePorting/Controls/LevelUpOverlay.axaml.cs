using System;
using System.Collections.Generic;
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
    private static readonly RadialGradientBrush WaveGradient = new()
    {
        GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
        RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop { Color = Color.Parse("#66953bf8"), Offset = 0 },
            new GradientStop { Color = Color.Parse("#00953bf8"), Offset = 0.7 }
        }
    };

    private static readonly RadialGradientBrush ParticleGradient = new()
    {
        GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
        RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop { Color = Color.Parse("#66953bf8"), Offset = 0.5 },
            new GradientStop { Color = Color.Parse("#00953bf8"), Offset = 1 }
        }
    };

    private static readonly double[] ParticleAngles = new double[30];
    
    static LevelUpOverlay()
    {
        for (var i = 0; i < 30; i++)
        {
            ParticleAngles[i] = (Math.PI * 2 * i) / 30;
        }
    }

    private readonly List<Ellipse> _wavePool = new(3);
    private readonly List<Ellipse> _particlePool = new(30);
    
    private readonly List<AnimatedElement> _activeAnimations = new();
    private DispatcherTimer _centralTimer;
    private double _centerX, _centerY;

    public LevelUpOverlay()
    {
        InitializeComponent();
        InitializePools();
    }

    private void InitializePools()
    {
        for (var i = 0; i < 3; i++)
        {
            var wave = new Ellipse
            {
                Fill = WaveGradient,
                IsVisible = false
            };
            _wavePool.Add(wave);
            WaveCanvas.Children.Add(wave);
        }

        for (var i = 0; i < 30; i++)
        {
            var particle = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = ParticleGradient,
                IsVisible = false
            };
            _particlePool.Add(particle);
            ParticleCanvas.Children.Add(particle);
        }
    }

    public async Task ShowLevelUp(int level)
    {
        LevelText.Text = level.ToString();
        
        IsVisible = true;
        
        await Task.Delay(100);
        
        _centerX = WaveCanvas.Bounds.Width / 2;
        _centerY = WaveCanvas.Bounds.Height / 2;
        
        OverlayBackground.Opacity = 0;
        OverlayBackground.Classes.Remove("Show");
        BurstContent.Opacity = 0;
        BurstContent.RenderTransform = new ScaleTransform(0.7, 0.7);
        
        StopAllAnimations();
        
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

    private void StopAllAnimations()
    {
        _centralTimer?.Stop();
        _activeAnimations.Clear();
        
        foreach (var wave in _wavePool)
        {
            wave.IsVisible = false;
        }
        foreach (var particle in _particlePool)
        {
            particle.IsVisible = false;
        }
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
                    Setters = { new Setter(OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(OpacityProperty, 0.0) }
                }
            }
        };

        await animation.RunAsync(OverlayBackground);
    }

    private async void AnimateContent()
    {
        var contentAnim = new ContentAnimation(BurstContent, DateTime.Now);
        _activeAnimations.Add(contentAnim);
        StartCentralTimer();
        
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
                    Setters = { new Setter(OpacityProperty, 0.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(OpacityProperty, 1.0) }
                }
            }
        };

        await animation.RunAsync(BurstContent);
    }

    private void CreateEnergyWaves()
    {
        TaskService.RunDispatcher(() =>
        {
            for (var i = 0; i < 3; i++)
            {
                var wave = _wavePool[i];
                wave.Width = 0;
                wave.Height = 0;
                wave.Opacity = 0;
                wave.IsVisible = true;
                
                Canvas.SetLeft(wave, _centerX);
                Canvas.SetTop(wave, _centerY);
                
                var delay = i * 300;
                var waveAnim = new WaveAnimation(wave, _centerX, _centerY, DateTime.Now.AddMilliseconds(delay));
                _activeAnimations.Add(waveAnim);
            }
            
            StartCentralTimer();
        }, DispatcherPriority.Render);
    }

    private void CreateParticles()
    {
        var random = new Random();
        
        TaskService.RunDispatcher(() =>
        {
            for (var i = 0; i < 30; i++)
            {
                var particle = _particlePool[i];
                var angle = ParticleAngles[i];
                var distance = 150 + random.NextDouble() * 100;
                
                particle.Opacity = 0;
                particle.IsVisible = true;
                particle.RenderTransform = new ScaleTransform(1, 1);
                
                Canvas.SetLeft(particle, _centerX - 4);
                Canvas.SetTop(particle, _centerY - 4);
                
                var delay = random.Next(0, 200);
                var particleAnim = new ParticleAnimation(
                    particle, angle, distance, _centerX, _centerY, 
                    DateTime.Now.AddMilliseconds(delay)
                );
                _activeAnimations.Add(particleAnim);
            }
            
            StartCentralTimer();
        }, DispatcherPriority.Render);
    }

    private void StartCentralTimer()
    {
        if (_centralTimer == null)
        {
            _centralTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _centralTimer.Tick += OnCentralTimerTick;
        }
        
        if (!_centralTimer.IsEnabled)
        {
            _centralTimer.Start();
        }
    }

    private void OnCentralTimerTick(object sender, EventArgs e)
    {
        var now = DateTime.Now;
        
        for (var i = _activeAnimations.Count - 1; i >= 0; i--)
        {
            if (!_activeAnimations[i].Update(now))
            {
                _activeAnimations.RemoveAt(i);
            }
        }
        
        if (_activeAnimations.Count == 0)
        {
            _centralTimer?.Stop();
        }
    }

    private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    private abstract class AnimatedElement(DateTime startTime, TimeSpan duration)
    {
        protected readonly DateTime StartTime = startTime;
        protected readonly TimeSpan Duration = duration;

        public abstract bool Update(DateTime now);

        protected double GetProgress(DateTime now)
        {
            if (now < StartTime) return -1;
            var elapsed = now - StartTime;
            return Math.Min(elapsed.TotalSeconds / Duration.TotalSeconds, 1.0);
        }
    }

    private class ContentAnimation(Border content, DateTime startTime)
        : AnimatedElement(startTime, TimeSpan.FromSeconds(0.6))
    {
        public override bool Update(DateTime now)
        {
            var progress = GetProgress(now);
            if (progress < 0) return true;
            if (progress >= 1.0) return false;

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

            content.RenderTransform = new ScaleTransform(scale, scale);
            return true;
        }
    }

    private class WaveAnimation(Ellipse wave, double centerX, double centerY, DateTime startTime)
        : AnimatedElement(startTime, TimeSpan.FromSeconds(1.5))
    {
        public override bool Update(DateTime now)
        {
            var progress = GetProgress(now);
            if (progress < 0) return true;
            if (progress >= 1.0)
            {
                wave.IsVisible = false;
                return false;
            }

            var eased = EaseOutCubic(progress);
            var size = eased * 800;
            
            wave.Width = size;
            wave.Height = size;
            
            Canvas.SetLeft(wave, centerX - size / 2);
            Canvas.SetTop(wave, centerY - size / 2);
            
            wave.Opacity = progress < 0.2 
                ? (progress / 0.2) * 0.8 
                : 0.8 * (1 - ((progress - 0.2) / 0.8));

            return true;
        }
    }

    private class ParticleAnimation(
        Ellipse particle,
        double angle,
        double distance,
        double centerX,
        double centerY,
        DateTime startTime)
        : AnimatedElement(startTime, TimeSpan.FromSeconds(1))
    {
        public override bool Update(DateTime now)
        {
            var progress = GetProgress(now);
            if (progress < 0) return true;
            if (progress >= 1.0)
            {
                particle.IsVisible = false;
                return false;
            }

            var eased = EaseOutCubic(progress);
            
            var currentX = Math.Cos(angle) * distance * eased;
            var currentY = Math.Sin(angle) * distance * eased;
            
            Canvas.SetLeft(particle, centerX + currentX - 4);
            Canvas.SetTop(particle, centerY + currentY - 4);
            
            particle.Opacity = progress < 0.2 
                ? progress / 0.2 
                : 1 - ((progress - 0.2) / 0.8);
            
            var scale = 1 - (eased * 0.5);
            particle.RenderTransform = new ScaleTransform(scale, scale);

            return true;
        }
    }
}