using Avalonia.Controls;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Navigation.Sidebar;

public partial class SidebarItemText : UserControl, ISidebarItem
{
    [AvaDirectProperty] private string _text;

    public SidebarItemText()
    {
        InitializeComponent();
    }
    
    public SidebarItemText(string text = "") : this()
    {
        Text = text;
    }
}