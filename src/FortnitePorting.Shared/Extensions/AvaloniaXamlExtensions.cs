using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Shared.Extensions;

public static class AvaloniaXamlExtensions
{
    extension(string text)
    {
        public T CreateXaml<T>(dynamic bindings) where T : Control
        {
            var content = AvaloniaRuntimeXamlLoader.Parse<T>(text);
            content.DataContext = bindings;
            return content;
        }
    }
}
