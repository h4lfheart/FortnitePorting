using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWPlayerExplosion : TWActor
{
    [ObservableProperty] private SolidColorBrush _brush;
    
    private static Color Color1 = Color.Parse("#B9A137");
    private static Color Color2 = Color.Parse("#2e2e2e");

    public TWPlayerExplosion()
    {
        IsActive = false;
    }

    public void Execute(TWVector2 pos)
    {
        IsActive = true;
        Time = 0;
        Position = pos;
    }

    public override void Update()
    {
        base.Update();
        
        if (!IsActive) return;
        
        var factor = Math.Clamp(Time, 0, 1);
        Brush = new SolidColorBrush(ImageExtensions.LerpColor(Color1, 1.0, Color2, 0.0, factor));
        Rotation += 0.075;

        if (Time >= 1)
        {
            IsActive = false;
        }
    }
}