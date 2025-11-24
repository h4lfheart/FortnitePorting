using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Settings;

public partial class SettingsItem : UserControl
{
    [AvaDirectProperty] private string _title;
    [AvaDirectProperty] private string _description;
    
    [AvaDirectProperty] private Control? _header;
    [AvaDirectProperty] private Control? _content;
    [AvaDirectProperty] private Control? _footer;

    public bool UseHeader => Header is not null;
    
    public SettingsItem()
    {
        InitializeComponent();
    }
}