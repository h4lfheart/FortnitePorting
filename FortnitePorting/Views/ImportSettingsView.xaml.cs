using System.Windows.Controls;
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

    private void OnRigTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppVM.ImportSettingsVM is null) return;
        
        if (AppVM.ImportSettingsVM.BlenderRigType.Equals(ERigType.Tasty))
        {
            AppVM.ImportSettingsVM.BlenderMergeSkeletons = true;
            AppVM.ImportSettingsVM.BlenderReorientBones = true;
        }
    }
}