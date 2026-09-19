using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Services;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWBoss : TWActor
{
    [ObservableProperty] private int _health;
    
    [ObservableProperty] private double _targetX;
    [ObservableProperty] private double _targetY;
    [ObservableProperty] private double _hitOpacity;
    [ObservableProperty] private EBossState _state = EBossState.Dead;

    private const int X_POS_MARGIN = 200;

    public TWBoss()
    {
        IsActive = false;
        Scale = new TWVector2(0.6);
    }

    public void Spawn()
    {
        IsActive = true;
        Health = 30;
        TargetX = 0;
        TargetY = TimeWasterVM.ViewportBounds.Height / -4;
        State = EBossState.Spawning;
    }
    
    public void Kill()
    {
        IsActive = false;
        TargetX = 0;
        TargetY = TimeWasterVM.ViewportBounds.Height / -4;
        State = EBossState.Dead;
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
        
        if (State is EBossState.Spawning && Math.Abs(TargetY - Position.Y) < 25)
        {
            State = EBossState.ReadyToAttack;
        }
        
        if (State is EBossState.Moving && Math.Abs(Position.X - TargetX) < 25)
        {
            State = EBossState.ReadyToAttack;
        }

        if (State is EBossState.Waiting)
        {
            TargetX = Random.Shared.Next((int) (-TimeWasterVM.ViewportBounds.Width / 2) + X_POS_MARGIN, (int) (TimeWasterVM.ViewportBounds.Width / 2) - X_POS_MARGIN);
            State = EBossState.Moving;
        }

        if (State is EBossState.ReadyToAttack)
        {
            State = EBossState.Attacking;

            TaskService.RunDispatcher(async () =>
            {
                var attackCount = Random.Shared.Next(1, 4);
                for (var i = 0; i < attackCount; i++)
                {
                    if (!IsActive) return;
                    CreatePineapple(0);
                    CreatePineapple(-45);
                    CreatePineapple(45);
                    await Task.Delay(1000);
                }
                State = EBossState.Waiting;
            });
        }
        
        Position.X = MoveTowards(Position.X, TargetX, 3);
        Position.Y = MoveTowards(Position.Y, TargetY, 3 * SmoothStep(Math.Abs(Position.Y - TargetY) / 100, 0, 1));
    }
    
    public void CreatePineapple(double angle)
    {
        var x = Math.Cos(Radians(angle - 90));
        var y = Math.Sin(Radians(angle - 90));
        
        var pineapple = new TWPineapple
        {
            Position =
            {
                X = Position.X,
                Y = Position.Y + 100
            },
            Rotation = angle,
            Velocity = new TWVector2(x, y) * -7.5
        };
        
        pineapple.Initialize();
        TimeWasterVM.Pineapples.Add(pineapple);
    }
    
    public static double Radians(double angle) 
    {
        return angle * Math.PI / 180; 
    }

}

public enum EBossState
{
    Dead,
    Spawning,
    Waiting,
    Moving,
    ReadyToAttack,
    Attacking
}