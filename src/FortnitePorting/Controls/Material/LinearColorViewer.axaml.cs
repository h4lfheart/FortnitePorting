using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CUE4Parse.UE4.Objects.Core.Math;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Material;

public readonly record struct LinearColorChannel(string Name, string Value);

public partial class LinearColorViewer : UserControl
{
    [AvaDirectProperty] private FLinearColor _value;
    [AvaDirectProperty] private IBrush _previewBrush = Brushes.Transparent;
    [AvaDirectProperty] private IReadOnlyList<LinearColorChannel> _channels = [];

    public LinearColorViewer()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property.Name == nameof(Value))
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var clamped = Value.ToFColor(false);
        PreviewBrush = new SolidColorBrush(new Color(clamped.A, clamped.R, clamped.G, clamped.B));
        Channels =
        [
            new("R", FormatFloat(Value.R)),
            new("G", FormatFloat(Value.G)),
            new("B", FormatFloat(Value.B)),
            new("A", FormatFloat(Value.A)),
            new("Hex", clamped.Hex),
        ];
    }

    private static string FormatFloat(float value) => value.ToString("G", CultureInfo.InvariantCulture);
}
