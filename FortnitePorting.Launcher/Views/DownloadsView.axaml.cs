using Avalonia.Controls;
using Avalonia.Interactivity;
using FortnitePorting.Launcher.Models.Downloads;
using FortnitePorting.Launcher.ViewModels;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Launcher.Views;

public partial class DownloadsView : ViewBase<DownloadsViewModel>
{
    public DownloadsView() : base(DownloadsVM, initializeViewModel: false)
    {
        InitializeComponent();
    }

    private void OnRepositoryFilterChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        if (checkBox.DataContext is not DownloadRepository downloadRepository) return;
        if (checkBox.IsChecked is not { } isChecked) return;
        if (isChecked == downloadRepository.IsFilterEnabled) return;
        
        downloadRepository.IsFilterEnabled = isChecked;
        
        ViewModel.FakeRefreshFilters();
    }
}