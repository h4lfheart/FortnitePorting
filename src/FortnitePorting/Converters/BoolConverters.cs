using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FortnitePorting.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool) value ? 1.0 : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (double)value > 0;
}
