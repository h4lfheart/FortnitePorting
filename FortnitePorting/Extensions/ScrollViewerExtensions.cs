using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace FortnitePorting.Extensions;

public class ScrollViewerMarginConverter : IValueConverter
{
    private static readonly Thickness _zeroThickness = new(0);
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ScrollViewer scrollViewer) return _zeroThickness;
        
        return scrollViewer.Extent.Height > scrollViewer.Viewport.Height ? new Thickness(0, 0, 16, 0) : _zeroThickness;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
