using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace FortnitePorting.Controls;

public class CustomBlurBehind : Control
{
    public static readonly StyledProperty<float> RadiusProperty = AvaloniaProperty.Register<CustomBlurBehind, float>(nameof(Radius));

    public float Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    private static ImmutableExperimentalAcrylicMaterial Material;

    public CustomBlurBehind()
    {
        Material = (ImmutableExperimentalAcrylicMaterial) new ExperimentalAcrylicMaterial
        {
            BackgroundSource = AcrylicBackgroundSource.Digger,
            MaterialOpacity = 1.5,
            TintColor = AppWM.Theme.BackgroundColor,
            TintOpacity = 1
        }.ToImmutable();
    }

    static CustomBlurBehind()
    {
        AffectsRender<CustomBlurBehind>(RadiusProperty);
    }
    
    public override void Render(DrawingContext context)
    {
        context.Custom(new BlurBehindRenderOperation(Material, Radius,new Rect(default, Bounds.Size)));
    }
}

file class BlurBehindRenderOperation(ImmutableExperimentalAcrylicMaterial Material, float Radius, Rect _bounds) : ICustomDrawOperation
{
    public Rect Bounds => _bounds;

    
    private static readonly SKShader AcrylicNoiseShader;
    private const double NoiseOpacity = 0.0225;

    static BlurBehindRenderOperation()
    {
        using var stream = typeof(SkiaPlatform).Assembly.GetManifestResourceStream("Avalonia.Skia.Assets.NoiseAsset_256X256_PNG.png");
        using var bitmap = SKBitmap.Decode(stream);
        
        AcrylicNoiseShader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
            .WithColorFilter(CreateAlphaColorFilter(NoiseOpacity));
    }
    
    public bool HitTest(Point p) => Bounds.Contains(p);
    public void Render(ImmediateDrawingContext context)
    {
        var skiaFeature = context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>();
        using var skia = skiaFeature?.Lease();
        if (skia?.SkSurface is null) return;
        
        using var backgroundSnapshot = skia.SkSurface.Snapshot();
        using var backdropShader = SKShader.CreateImage(backgroundSnapshot, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, skia.SkCanvas.TotalMatrix.Invert());
        
        using var blurred = SKSurface.Create(skia.GrContext, false, new SKImageInfo((int) Math.Ceiling(Bounds.Width), (int) Math.Ceiling(Bounds.Height), SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using var filter = SKImageFilter.CreateBlur(3, 3, SKShaderTileMode.Clamp);
        using var blurPaint = new SKPaint();
        blurPaint.Shader = backdropShader;
        blurPaint.ImageFilter = filter;
        blurred.Canvas.DrawRoundRect(0, 0, (float)Bounds.Width, (float)Bounds.Height, 0, 0, blurPaint);
        
        using var blurSnap = blurred.Snapshot();
        using var blurSnapShader = SKShader.CreateImage(blurSnap);
        using var blurSnapPaint = new SKPaint();
        blurSnapPaint.Shader = blurSnapShader;
        blurSnapPaint.IsAntialias = true;
        skia.SkCanvas.DrawRoundRect(0, 0, (float) Bounds.Width, (float) Bounds.Height, 0, 0, blurSnapPaint);

        var tintColor = Material.TintColor;
        var tint = new SKColor(tintColor.R, tintColor.G, tintColor.B, tintColor.A);

        using var backdrop = SKShader.CreateColor(new SKColor(Material.MaterialColor.R, Material.MaterialColor.G, Material.MaterialColor.B, 128));
        using var tintShader = SKShader.CreateColor(tint);
        using var effectiveTint = SKShader.CreateCompose(backdrop, tintShader);
        using var compose = SKShader.CreateCompose(effectiveTint, AcrylicNoiseShader);
        
        using var acrylicPaint = new SKPaint();
        acrylicPaint.IsAntialias = true;
        acrylicPaint.Shader = compose;
        skia.SkCanvas.DrawRoundRect(0, 0, (float) Bounds.Width, (float) Bounds.Height, 0, 0, acrylicPaint);
    }


    private static SKColorFilter CreateAlphaColorFilter(double opacity)
    {
        if (opacity > 1)
            opacity = 1;
        var c = new byte[256];
        var a = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            c[i] = (byte)i;
            a[i] = (byte)(i * opacity);
        }

        return SKColorFilter.CreateTable(a, c, c, c);
    }
    
    public bool Equals(ICustomDrawOperation? other)
    {
        return other == this;
    }
    
    public void Dispose()
    {
        
    }
}