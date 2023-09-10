using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace FortnitePorting.Framework;

public class ViewTransition : IPageTransition
{
    public TimeSpan Duration = TimeSpan.FromSeconds(0.5);
    
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (from is null || to is null) return;
        
        from.IsVisible = false;
        var animation = new Animation
        {
            FillMode = FillMode.Forward,
            Duration = Duration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Visual.OpacityProperty, 
                            Value = 0.0
                        },
                        new Setter
                        {
                            Property = TranslateTransform.XProperty, 
                            Value = 250
                        }
                    },
                    Cue = new Cue(0.0)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = Visual.OpacityProperty,
                            Value = 1.0
                        },
                        new Setter
                        {
                            Property = TranslateTransform.XProperty, 
                            Value = 0
                        }
                    },
                    Cue = new Cue(1.0)
                }
            }
        };
        await animation.RunAsync(to, cancellationToken).ConfigureAwait(false);
    }
}