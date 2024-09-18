using System;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWBoss : TWActor
{
    [ObservableProperty] private int _health;
    
    [ObservableProperty] private double _targetX;
    [ObservableProperty] private double _targetY;
    [ObservableProperty] private double _hitOpacity;

    public bool ReadyToFight => Math.Abs(TargetY - Position.Y) < 0.01f;

    public TWBoss()
    {
        IsActive = false;
        Scale = new TWVector2(0.6);
    }

    public void Spawn()
    {
        IsActive = true;
        Health = 5;
        TargetX = 0;
        TargetY = TimeWasterVM.ViewportBounds.Height / -4;
    }
    
    public void Kill()
    {
        IsActive = false;
        TargetX = 0;
        TargetY = TimeWasterVM.ViewportBounds.Height / -4;
    }

    public override void Update()
    {
        base.Update();

        if (!IsActive)
        {
            Position.X = 0;
            Position.Y = TimeWasterVM.ViewportBounds.Height / -2 - 400;
            return;
        }
        
        Position.X = MoveTowards(Position.X, TargetX, 3 * SmoothStep(Math.Abs(Position.X - TargetX) / 100, 0, 1));
        Position.Y = MoveTowards(Position.Y, TargetY, 3 * SmoothStep(Math.Abs(Position.Y - TargetY) / 100, 0, 1));
    }

}