using System;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWProjectile : TWActor
{
    public override void Update()
    {
        base.Update();
        
        var spawnScalar = float.Lerp(0.25f, 1, Math.Clamp(Time * 5, 0, 1.0f));
        Scale.X = spawnScalar * (Math.Sin(Time * 10) * 0.1 + 1);
        Position.Y -= 8;
    }
}