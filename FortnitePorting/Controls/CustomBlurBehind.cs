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
    public static readonly StyledProperty<ExperimentalAcrylicMaterial> MaterialProperty = AvaloniaProperty.Register<CustomBlurBehind, ExperimentalAcrylicMaterial>(
        "Material");

    public ExperimentalAcrylicMaterial Material
    {
        get => GetValue(MaterialProperty);
        set => SetValue(MaterialProperty, value);
    }

    static ImmutableExperimentalAcrylicMaterial DefaultAcrylicMaterial = (ImmutableExperimentalAcrylicMaterial)new ExperimentalAcrylicMaterial()
    {
        MaterialOpacity = 0.5,
        TintColor = Colors.Azure,
        TintOpacity = 0.5,
        PlatformTransparencyCompensationLevel = 0
    }.ToImmutable();
    
    static CustomBlurBehind()
    {
        AffectsRender<CustomBlurBehind>(MaterialProperty);
    }
    
    private static SKShader s_acrylicNoiseShader;
    class BlurBehindRenderOperation : ICustomDrawOperation
    {
        private readonly ImmutableExperimentalAcrylicMaterial _material;
        private readonly Rect _bounds;

        public BlurBehindRenderOperation(ImmutableExperimentalAcrylicMaterial material, Rect bounds)
        {
            _material = material;
            _bounds = bounds;
        }
        
        public void Dispose()
        {
            
        }

        // todo refactor
        public bool HitTest(Point p) => _bounds.Contains(p);
        public void Render(ImmediateDrawingContext context)
        {
            var skiaFeature = context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>();
            using var skia = skiaFeature.Lease();
            
            if(!skia.SkCanvas.TotalMatrix.TryInvert(out var currentInvertedTransform))
                return;
            
            
            using var backgroundSnapshot = skia.SkSurface.Snapshot();
            using var backdropShader = SKShader.CreateImage(backgroundSnapshot, SKShaderTileMode.Clamp,
                SKShaderTileMode.Clamp, currentInvertedTransform);
            
            if (skia.GrContext == null)
            {
                using (var filter = SKImageFilter.CreateBlur(3, 3, SKShaderTileMode.Clamp))
                using (var tmp = new SKPaint()
                       {
                           Shader = backdropShader,
                           ImageFilter = filter
                       })
                    skia.SkCanvas.DrawRoundRect(0, 0, (float)_bounds.Width, (float)_bounds.Height, 4, 4, tmp);

                return;
            }
            
            using var blurred = SKSurface.Create(skia.GrContext, false, new SKImageInfo(
                (int)Math.Ceiling(_bounds.Width),
                (int)Math.Ceiling(_bounds.Height), SKImageInfo.PlatformColorType, SKAlphaType.Premul));
            using(var filter = SKImageFilter.CreateBlur(3, 3, SKShaderTileMode.Clamp))
            using (var blurPaint = new SKPaint
                   {
                       Shader = backdropShader,
                       ImageFilter = filter
                   })
                blurred.Canvas.DrawRoundRect(0, 0, (float)_bounds.Width, (float)_bounds.Height, 4, 4, blurPaint);
            using (var blurSnap = blurred.Snapshot())
                using(var blurSnapShader = SKShader.CreateImage(blurSnap))
                using (var blurSnapPaint = new SKPaint
                       {
                           Shader = blurSnapShader,
                           IsAntialias = true
                       })
                    skia.SkCanvas.DrawRoundRect(0, 0, (float)_bounds.Width, (float)_bounds.Height, 4, 4, blurSnapPaint);

                //return;
            using var acrylliPaint = new SKPaint();
            acrylliPaint.IsAntialias = true;
            
            double opacity = 1;

            const double noiseOpacity = 0.0225;

            var tintColor = _material.TintColor;
            var tint = new SKColor(tintColor.R, tintColor.G, tintColor.B, tintColor.A);

            if (s_acrylicNoiseShader == null)
            {
                using (var stream = typeof(SkiaPlatform).Assembly.GetManifestResourceStream("Avalonia.Skia.Assets.NoiseAsset_256X256_PNG.png"))
                using (var bitmap = SKBitmap.Decode(stream))
                {
                    s_acrylicNoiseShader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
                        .WithColorFilter(CreateAlphaColorFilter(noiseOpacity));
                }
            }

            using (var backdrop = SKShader.CreateColor(new SKColor(_material.MaterialColor.R, _material.MaterialColor.G, _material.MaterialColor.B, _material.MaterialColor.A)))
            using (var tintShader = SKShader.CreateColor(tint))
            using (var effectiveTint = SKShader.CreateCompose(backdrop, tintShader))
            using (var compose = SKShader.CreateCompose(effectiveTint, s_acrylicNoiseShader))
            {
                acrylliPaint.Shader = compose;
                acrylliPaint.IsAntialias = true;
                skia.SkCanvas.DrawRoundRect(0, 0, (float)_bounds.Width, (float)_bounds.Height, 4, 4, acrylliPaint);
            }
        }


        static SKColorFilter CreateAlphaColorFilter(double opacity)
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
        

        public Rect Bounds => _bounds.Inflate(4);
        public bool Equals(ICustomDrawOperation? other)
        {
            return other is BlurBehindRenderOperation op && op._bounds == _bounds && op._material.Equals(_material);
        }
    }
    
    
    public override void Render(DrawingContext context)
    {
        var mat = Material != null
            ? (ImmutableExperimentalAcrylicMaterial)Material.ToImmutable()
            : DefaultAcrylicMaterial;
        context.Custom(new BlurBehindRenderOperation(mat, new Rect(default, Bounds.Size)));
    }
}