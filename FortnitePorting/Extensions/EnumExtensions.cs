using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Material.Icons;

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
        
        public MaterialIconKind? Icon =>
            value.GetType()
                .GetField(value.ToString())?
                .GetCustomAttributes(typeof(IconAttribute), false).SingleOrDefault() is not IconAttribute attribute ? null : attribute.Icon;

        public EnumRecord ToEnumRecord()
        {
            return new EnumRecord(value.GetType(), value, value.Description, value.IsDisabled, value.Icon);
        }
    }
}

public class EnumToItemsSource(Type type) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var values = Enum.GetValues(type).Cast<Enum>();
        return values.Select(value => value.ToEnumRecord()).ToList();
    }
}

public class DisabledAttribute : Attribute;

public class IconAttribute(MaterialIconKind icon) : Attribute
{
    public MaterialIconKind Icon = icon;
}

public record EnumRecord(Type EnumType, Enum Value, string Description, bool IsDisabled = false, MaterialIconKind? Icon = null)
{
    public override string ToString()
    {
        return Description;
    }
}
