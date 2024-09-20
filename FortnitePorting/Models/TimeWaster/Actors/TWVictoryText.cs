using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWVictoryText : TWActor
{
    [ObservableProperty] private double _opacity = 1.0;
    
    public TWVictoryText()
    {
        IsActive = false;
        Position.Y = -125;
        Scale.X = 0;
        Scale.Y = 0;
    }
    
    public void Display()
    {
        Scale.X = 0;
        Scale.Y = 0;
        Time = 0;
        Opacity = 1;
        
        UpdateTransforms();
        
        IsActive = true;
    }
    
    public override void Update()
    {
        base.Update();
        
        if (!IsActive) return;
        
        Scale.X = MoveTowards(Scale.X, 1.0, 350 * SmoothStep(Math.Abs(Scale.X - 1.0) / 50, 0, 1));
        Scale.Y = MoveTowards(Scale.Y, 1.0, 350 * SmoothStep(Math.Abs(Scale.Y - 1.0) / 50, 0, 1));

        var factor = (Math.Clamp(Time, 3, 3.5) - 3) / 0.5;
        Opacity = double.Lerp(1, 0, factor);
    }
}