using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWPineapple : TWActor
{
    [ObservableProperty] private TWVector2 _velocity = TWVector2.Zero;
    
    public TWPineapple()
    {
        Scale = new TWVector2(0.85);
    }

    public override void Update()
    {
        base.Update();

        Position += Velocity;
    }
}