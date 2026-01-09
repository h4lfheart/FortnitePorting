using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace FortnitePorting.Converters;


public class ConditionalConverter<T> : IValueConverter
{
    public T? TrueValue { get; set; }
    public T? FalseValue { get; set; }
    public object? Threshold { get; set; }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool condition = EvaluateCondition(value);
        return condition ? TrueValue : FalseValue;
    }
    
    private bool EvaluateCondition(object? value)
    {
        if (value is bool b) return b;
        if (value is int i) return Threshold != null ? i > System.Convert.ToInt32(Threshold) : i > 0;
        if (value is double d) return Threshold != null ? d > System.Convert.ToDouble(Threshold) : d > 0;
        if (value is string s) return !string.IsNullOrEmpty(s);
        if (value is ICollection collection) return collection.Count > 0;
        return value != null;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GridLengthConverter : ConditionalConverter<GridLength>;