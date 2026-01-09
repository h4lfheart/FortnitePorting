using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Navigation.Sidebar;

public partial class SidebarItemContent : UserControl, ISidebarItem
{
    [AvaDirectProperty] private Control? _content;
    
    public SidebarItemContent()
    {
        InitializeComponent();
    }
}