using Avalonia.Controls;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class WelcomeView : ViewBase<WelcomeViewModel>
{
    public WelcomeView()
    {
        InitializeComponent();
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl) return;
        if (tabControl.SelectedItem is not TabItem tabItem) return;
        if (tabItem.Tag is not ELoadingType loadingType) return;
        ViewModel.CurrentLoadingType = loadingType;
    }
}