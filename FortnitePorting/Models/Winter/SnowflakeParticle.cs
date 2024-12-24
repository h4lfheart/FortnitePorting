using System;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Winter;

public partial class SnowflakeParticle : ObservableObject
{
    public MatrixTransform SnowflakeTransform =>
        new(Matrix.CreateScale(Scale, Scale) * Matrix.CreateTranslation(XPosition, YPosition));

    public const float DELTA_TIME = 1f / 60f;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SnowflakeTransform))] private float _xPosition;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SnowflakeTransform))] private float _yPosition;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SnowflakeTransform))] private float _scale;
    [ObservableProperty] private float _opacity = 1.0f;
    
    private readonly float _speed;
    private readonly int _sign;
    
    private float _personalTime;

    public SnowflakeParticle(float speed, float xPosition, float yPosition)
    {
        XPosition = xPosition;
        YPosition = yPosition;
        Scale = 1.0f;
        Opacity = (0.1f + Random.Shared.NextSingle()) / 3;
        
        _speed = speed;
        _sign = Random.Shared.NextSingle() < 0.5f ? 1 : -1;
    }

    public void Update()
    {
        _personalTime += _speed * DELTA_TIME;

        XPosition += MathF.Cos(_personalTime) * _speed * DELTA_TIME * _sign * 50;
        YPosition += MathF.Sin(_personalTime) / 50 + _speed * DELTA_TIME * 50;
        Scale = 0.5f + MathF.Abs(10 * MathF.Cos(_personalTime) / 20);

    }
}