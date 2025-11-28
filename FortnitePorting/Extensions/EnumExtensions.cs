using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Extensions;


public static class EnumExtensions
{
    extension(Enum value)
    {
        public string Description =>
            value.GetType()
                .GetField(value.ToString())?
                .GetCustomAttributes(typeof(DescriptionAttribute), false).SingleOrDefault() is not DescriptionAttribute attribute ? value.ToString() : attribute.Description;

        public bool IsDisabled =>
            value.GetType()
                .GetField(value.ToString())?.GetCustomAttributes(typeof(DisabledAttribute), false).SingleOrDefault() is not null;
        
    }
}

public class EnumToItemsSource(Type type) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var values = Enum.GetValues(type).Cast<Enum>();
        return values.Select(value => new EnumRecord(type, value, value.Description, value.IsDisabled)).ToList();
    }
}

public class DisabledAttribute : Attribute;

public record EnumRecord(Type EnumType, Enum Value, string Description, bool IsDisabled = false)
{
    public override string ToString()
    {
        return Description;
    }
}
