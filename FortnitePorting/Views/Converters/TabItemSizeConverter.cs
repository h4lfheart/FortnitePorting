using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace FortnitePorting.Views.Converters;

public class TabItemSizeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not TabControl tabControl)
            return 0;

        var width = tabControl.ActualWidth / tabControl.Items.Count;
        return width <= 1 ? 0 : width - 1;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}