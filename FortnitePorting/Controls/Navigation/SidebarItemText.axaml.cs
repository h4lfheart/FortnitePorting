using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Navigation;

public partial class SidebarItemText : UserControl, ISidebarItem
{
    [AvaDirectProperty] private string _text;
    
    public SidebarItemText()
    {
        InitializeComponent();
    }
}