using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class AssetsView : ViewBase<AssetsViewModel>
{
    public AssetsView()
    {
        InitializeComponent();
    }
}