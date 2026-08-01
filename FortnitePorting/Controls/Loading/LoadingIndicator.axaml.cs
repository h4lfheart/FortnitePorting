using Avalonia.Controls;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Loading;

public partial class LoadingIndicator : UserControl
{
    [AvaDirectProperty] private string _status = string.Empty;
    [AvaDirectProperty] private string _subtitle = string.Empty;
    [AvaDirectProperty] private double _value;
    [AvaDirectProperty] private double _maximum;
    [AvaDirectProperty] private bool _isIndeterminate;

    public LoadingIndicator()
    {
        InitializeComponent();
    }
}
