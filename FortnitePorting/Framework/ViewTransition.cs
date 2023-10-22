using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace FortnitePorting.Framework;

public class ViewTransition : IPageTransition
{
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var distance = from.Bounds.Width;
        var translateProperty = TranslateTransform.XProperty;

        from.IsVisible = false;
        var animation = new Animation
        {
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = translateProperty,
                            Value = forward ? distance : -distance
                        },
                        new Setter
                        {
                            Property = Visual.OpacityProperty,
                            Value = 0
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = translateProperty, 
                            Value = 0
                        },
                        new Setter
                        {
                            Property = Visual.OpacityProperty,
                            Value = 1
                        }
                    },
                    Cue = new Cue(1)
                }
            },
            Duration = TimeSpan.FromSeconds(0.3)
        };

        await animation.RunAsync(to, cancellationToken);
    }
}