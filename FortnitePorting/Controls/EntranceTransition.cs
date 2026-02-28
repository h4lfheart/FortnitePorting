using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace FortnitePorting.Controls;

public class EntranceTransition : IPageTransition
{
    
    private double FromHorizontalOffset { get; set; } = 0;
    private double FromVerticalOffset { get; set; } = 100;
    private TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0.5);

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (to is null) return;
      
        if (from is not null && from != to)
            from.Opacity = 0;

        var animation = new Animation
        {
            Easing = new SplineEasing(0.1, 0.9, 0.2, 1.0),
            FillMode = FillMode.Forward,
            Duration = Duration,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                        new Setter(TranslateTransform.XProperty, FromHorizontalOffset),
                        new Setter(TranslateTransform.YProperty, FromVerticalOffset)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                        new Setter(TranslateTransform.XProperty, 0.0),
                        new Setter(TranslateTransform.YProperty, 0.0)
                    }
                }
            }
        };

        try
        {
            await animation.RunAsync(to, cancellationToken);
        }
        finally
        {
            to.Opacity = 1;
            to.RenderTransform = null;
        }
    }
}