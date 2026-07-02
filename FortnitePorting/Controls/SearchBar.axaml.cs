using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace FortnitePorting.Controls;

public partial class SearchBar : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<SearchBar, string>(nameof(Text), string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<SearchBar, string>(nameof(Watermark), string.Empty);

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public static readonly IValueConverter StringNotEmptyConverter =
        new FuncValueConverter<string?, bool>(s => !string.IsNullOrEmpty(s));

    public SearchBar()
    {
        InitializeComponent();
    }

    private void OnClearPressed(object? sender, PointerPressedEventArgs e)
    {
        Text = string.Empty;
        PART_TextBox.Focus();
    }
}
