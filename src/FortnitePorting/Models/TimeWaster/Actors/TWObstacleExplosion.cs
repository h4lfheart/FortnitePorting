using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWObstacleExplosion : TWActor
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ImageBitmap))] private bool _isKetchup;
    [ObservableProperty] private SolidColorBrush _textBrush;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowScore))] private int _score;
    [ObservableProperty] private double _scale;
    
    [ObservableProperty] private TWActor _spriteActor = new();

    public bool ShowScore => Score > 0;

    public Bitmap ImageBitmap => IsKetchup ? Ketchup : Mustard;
    
    private static Bitmap Ketchup = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/TimeWaster/Sprites/T_Ketchup.png");
    private static Bitmap Mustard = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/TimeWaster/Sprites/T_Mustard.png");
    private static Color Color1 = Color.Parse("#63e0e4");
    private static Color Color2 = Color.Parse("#eddf76");

    public override void Update()
    {
        base.Update();
        
        if (Math.Round(Time * 100) % 5 == 0)
        {
            IsKetchup = !IsKetchup;
            SpriteActor.Rotation = (Random.Shared.NextDouble() * 2 - 1) * 180;
        }

        var factor = Math.Clamp(Time / 0.5, 0, 1);
        SpriteActor.Scale = new TWVector2(double.Lerp(0.25 * Scale, 0.8 * Scale, factor));
        TextBrush = new SolidColorBrush(ImageExtensions.LerpColor(Color1, Color2, factor));
        
        SpriteActor.Update();
    }

    public override void Initialize()
    {
        base.Initialize();

        SpriteActor.Scale = new TWVector2(0.25 * Scale);
        SpriteActor.Rotation = (Random.Shared.NextDouble() * 2 - 1) * 180;
        
        SpriteActor.Initialize();
    }
    
}