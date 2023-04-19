using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using FortnitePorting.AppUtils;

namespace FortnitePorting.Views.Controls;

public partial class TextWithIcon
{
    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(TextWithIcon));

    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(int), typeof(TextWithIcon), new PropertyMetadata(24));

    public int IconSize
    {
        get => (int)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.Register(nameof(LabelFontSize), typeof(int), typeof(TextWithIcon));

    public int LabelFontSize
    {
        get => (int)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }
    
    public static readonly DependencyProperty LabelFontWeightProperty = DependencyProperty.Register(nameof(LabelFontWeight), typeof(FontWeight), typeof(TextWithIcon), new PropertyMetadata(FontWeights.Normal));

    public FontWeight LabelFontWeight
    {
        get => (FontWeight)GetValue(LabelFontWeightProperty);
        set => SetValue(LabelFontWeightProperty, value);
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(TextWithIcon));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public TextWithIcon()
    {
        InitializeComponent();
        if (AppSettings.Current.LightMode)
            IconImage.Effect = new InvertEffect();
    }

    public TextWithIcon(bool isProp = false)
    {
        InitializeComponent();
        if (AppSettings.Current.LightMode && !isProp)
            IconImage.Effect = new InvertEffect();
    }
}

class InvertEffect : ShaderEffect
{
    private const string _kshaderAsBase64 = @"AAP///7/IQBDVEFCHAAAAE8AAAAAA///AQAAABwAAAAAAQAASAAAADAAAAADAAAAAQACADgAAAAAAAAAaW5wdXQAq6sEAAwAAQABAAEAAAAAAAAAcHNfM18wAE1pY3Jvc29mdCAoUikgSExTTCBTaGFkZXIgQ29tcGlsZXIgOS4yOS45NTIuMzExMQBRAAAFAAAPoAAAgD4AAAAAAAAAAAAAAAAfAAACBQAAgAAAA5AfAAACAAAAkAAID6BCAAADAAAPgAAA5JAACOSgBgAAAgEAAYAAAP+ABQAAAwAAB4AAAOSAAQAAgAUAAAMAAAeAAAD/gAAA5IABAAACAAgIgAAA/4AFAAADAAgHgAAA5IAAAACg//8AAA==";

    private static readonly PixelShader _shader;

    static InvertEffect()
    {
        _shader = new PixelShader();
        _shader.SetStreamSource(new MemoryStream(Convert.FromBase64String(_kshaderAsBase64)));
    }

    public InvertEffect()
    {
        PixelShader = _shader;
        UpdateShaderValue(InputProperty);
    }

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(InvertEffect), 0);
}