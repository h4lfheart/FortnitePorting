using System;
using System.Threading.Tasks;
using ATL.Logging;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Log = Serilog.Log;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWPlayer : TWActor
{
    [ObservableProperty] private TWVector2 _target = TWVector2.Zero;
    [ObservableProperty] private double _blinkOpacity = 1.0;
    [ObservableProperty] private double _boosterOpacity = 0.0;
    [ObservableProperty] private bool _dead = true;

    public bool FinishedSpawn => Math.Abs(Target.Y - Position.Y) < 10;
    private bool BlinkedSinceSpawn = true;
    public bool IsVulnerable = true;

    public override void Initialize()
    {
        Target.X = 0;
        Target.Y = (float) (TimeWasterVM.ViewportBounds.Height / 2 - 100);
        
        base.Initialize();
        
        Spawn();
    }
    
    public void Spawn()
    {
        Dead = false;
        Position.X = 0;
        Position.Y = Target.Y + 200;
        Time = 0;
        BlinkedSinceSpawn = false;
        
        UpdateTransforms();
    }
    
    public void Kill()
    {
        Dead = true;
        IsVulnerable = false;
    }

    public override void Update()
    {
        base.Update();
     
        if (Dead) return;
        
        Target.Y = (float) (TimeWasterVM.ViewportBounds.Height / 2 - 100);
        Position.Y = MoveTowards(Position.Y, Target.Y, 5 * SmoothStep(Math.Abs(Position.Y - Target.Y) / 100, 0, 1));
        if (!FinishedSpawn)
        {
            BoosterOpacity = double.Lerp(0, 1, Math.Clamp(Math.Abs(Position.Y - Target.Y) / 200 * 5, 0, 1));
        }
        else
        {
            BoosterOpacity = 0;
        }
        
        if (FinishedSpawn) Position.X = MoveTowards(Position.X, Target.X, 10 * SmoothStep(Math.Abs(Position.X - Target.X) / 100, 0, 1));

        if (FinishedSpawn && !BlinkedSinceSpawn)
        {
            BlinkedSinceSpawn = true;
            Blink();
        }
    }

    public void Blink()
    {
        TaskService.Run(async () =>
        {
            for (var i = 0; i < 4; i++)
            {
                BlinkOpacity = 0.0;
                await Task.Delay(150);
                BlinkOpacity = 1.0;
                await Task.Delay(150);
            }

            IsVulnerable = true;
        });
    }
    
}