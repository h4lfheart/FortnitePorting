using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Views.Converters;

public class EnumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var enumValue = (Enum) value;
        return enumValue.GetDescription();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var values = Enum.GetValues(targetType).Cast<Enum>();
        return values.FirstOrDefault(x => x.GetDescription().Equals(value)) ?? value;
    }
}