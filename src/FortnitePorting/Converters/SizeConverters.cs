using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FortnitePorting.Converters;

public class ScaleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float scale && parameter is string paramString)
        {
            if (float.TryParse(paramString, out var baseValue))
            {
                return (double)(baseValue * scale);
            }
        }
        
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}