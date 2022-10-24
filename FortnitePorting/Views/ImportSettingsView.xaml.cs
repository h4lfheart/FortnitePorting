using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ImportSettingsView
{
    public ImportSettingsView()
    {
        InitializeComponent();
        AppVM.ImportSettingsVM = new ImportSettingsViewModel();
        DataContext = AppVM.ImportSettingsVM;
    }
}