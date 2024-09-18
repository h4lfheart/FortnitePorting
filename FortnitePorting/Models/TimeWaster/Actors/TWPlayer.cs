using System;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWPlayer : TWActor
{
    [ObservableProperty] private TWVector2 _target = TWVector2.Zero;
    [ObservableProperty] private bool _dead = false;

    public override void Initialize()
    {
        Position.X = MoveTowards(Position.X, Target.X, 10 * SmoothStep(Math.Abs(Position.X - Target.X) / 100, 0, 1));
        Position.Y = (float) (TimeWasterVM.ViewportBounds.Height / 2 - 100);

        Target.X = Position.X;
        Target.Y = Position.Y;
        
        base.Initialize();
    }

    public override void Update()
    {
        base.Update();
     
        if (Dead) return;
        
        Position.X = MoveTowards(Position.X, Target.X, 10 * SmoothStep(Math.Abs(Position.X - Target.X) / 100, 0, 1));
        Position.Y = MoveTowards(Position.Y, Target.Y, 10 * SmoothStep(Math.Abs(Position.Y - Target.Y) / 100, 0, 1));
    }
    
}