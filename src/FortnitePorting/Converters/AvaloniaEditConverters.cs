using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaEdit.Document;

namespace FortnitePorting.Converters;

public class StringToDocumentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text) return null;

        return new TextDocument(text);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TextDocument document) return null;

        return document.Text;
    }
}

