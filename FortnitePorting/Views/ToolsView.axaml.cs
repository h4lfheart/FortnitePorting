using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ToolsView : ViewBase<ToolsViewModel>
{
    public ToolsView() : base(lateInit: true)
    {
        InitializeComponent();
    }
}