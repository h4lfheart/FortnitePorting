using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Lucdem.Avalonia.SourceGenerators.Attributes;
using Material.Icons;

namespace FortnitePorting.Controls.Navigation;

public partial class SidebarItemButton : UserControl, ISidebarItem
{
    [AvaDirectProperty] private string _text;
    [AvaDirectProperty] private MaterialIconKind _icon;
    [AvaStyledProperty] private bool _isSelected;
    
    public SidebarItemButton()
    {
        InitializeComponent();
    }
}