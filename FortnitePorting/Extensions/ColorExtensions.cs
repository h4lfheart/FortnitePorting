using Avalonia.Media;

namespace FortnitePorting.Extensions;

public static class ColorExtensions
{
    public static void SetSystemAccentColor(Color baseColor)
    {
        Avalonia.Application.Current!.Resources["SystemAccentColor"] = baseColor;
        Avalonia.Application.Current.Resources["SystemAccentColorDark1"] = baseColor.ChangeColorLuminosity(-0.3);
        Avalonia.Application.Current.Resources["SystemAccentColorDark2"] = baseColor.ChangeColorLuminosity(-0.5);
        Avalonia.Application.Current.Resources["SystemAccentColorDark3"] = baseColor.ChangeColorLuminosity(-0.7);
        Avalonia.Application.Current.Resources["SystemAccentColorLight"] = baseColor.ChangeColorLuminosity(0.3);
        Avalonia.Application.Current.Resources["SystemAccentColorLight"] = baseColor.ChangeColorLuminosity(0.5);
        Avalonia.Application.Current.Resources["SystemAccentColorLight"] = baseColor.ChangeColorLuminosity(0.7);
    }
    
    public static Color ChangeColorLuminosity(this Color color, double luminosityFactor)
    {
        var red = (double)color.R;
        var green = (double)color.G;
        var blue = (double)color.B;

        switch (luminosityFactor)
        {
            case < 0:
                luminosityFactor = 1 + luminosityFactor;
                red *= luminosityFactor;
                green *= luminosityFactor;
                blue *= luminosityFactor;
                break;
            case >= 0:
                red = (255 - red) * luminosityFactor + red;
                green = (255 - green) * luminosityFactor + green;
                blue = (255 - blue) * luminosityFactor + blue;
                break;
        }

        return new Color(color.A, (byte)red, (byte)green, (byte)blue);
    }
}