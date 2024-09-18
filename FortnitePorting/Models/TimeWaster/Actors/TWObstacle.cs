namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWObstacle : TWActor
{
    public double AngularVelocity;

    public TWObstacle()
    {
        Scale = new TWVector2(0.175, 0.175);
    }

    public override void Update()
    {
        base.Update();
        
        Position.Y += 4;
        Rotation += AngularVelocity;
    }
}