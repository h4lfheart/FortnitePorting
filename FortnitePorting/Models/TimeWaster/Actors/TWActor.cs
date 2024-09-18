using System;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.TimeWaster.Actors;

public partial class TWActor : ObservableObject
{
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private float _time;

    [ObservableProperty] private TWVector2 _position = TWVector2.Zero;
    [ObservableProperty] private TWVector2 _scale = TWVector2.One;
    [ObservableProperty] private double _rotation;

    [ObservableProperty] private MatrixTransform _renderTransform;

    public Matrix ObjectMatrix => Matrix.CreateScale(Scale.X, Scale.Y) 
                                  * Matrix.CreateRotation(Rotation) 
                                  * Matrix.CreateTranslation(Position.X, Position.Y);
    
    private const float DELTA_TIME = 1.0f / 60f;
    

    public virtual void Initialize()
    {
        UpdateTransforms(init: true);
    }

    public virtual void Update()
    {
        UpdateTransforms();
    }

    public void UpdateTransforms(bool init = false)
    {
        RenderTransform = new MatrixTransform(ObjectMatrix);
        if (!init)
        {
            Time += DELTA_TIME;
        }
    }

    protected static double SmoothStep(double x, double edge0, double edge1) 
    {
        x = (float) Math.Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0); 
        return x * x * (3 - 2 * x);
    }

    protected static double MoveTowards(double orig, double target, double amount)
    {
        double result;
        if (orig < target) 
        {
            result = orig + amount;
            if (result > target) 
            {
                result = target;
            }
        } 
        else if (orig > target) 
        {
            result = orig - amount;
            if (result < target) 
            {
                result = target;
            }
        } 
        else 
        {
            result = target;
        }
        return result;

    }
}

public partial class TWVector2 : ObservableObject
{
    public static TWVector2 Zero => new(0, 0);
    public static TWVector2 One => new(1, 1);
    
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;

    public TWVector2(double x, double y)
    {
        X = x;
        Y = y;
    }

    public TWVector2(double value)
    {
        X = value;
        Y = value;
    }

    public TWVector2 Copy()
    {
        return new TWVector2(X, Y);
    }
}