using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports.Blender;

namespace FortnitePorting.ViewModels;

public class ImportSettingsViewModel : ObservableObject
{
    public bool BlenderMergeSkeletons
    {
        get => AppSettings.Current.BlenderExportSettings.MergeSkeletons;
        set
        {
            AppSettings.Current.BlenderExportSettings.MergeSkeletons = value;
            OnPropertyChanged();
        }
    }
    
    public bool BlenderReorientBones
    {
        get => AppSettings.Current.BlenderExportSettings.ReorientBones;
        set
        {
            AppSettings.Current.BlenderExportSettings.ReorientBones = value;
            OnPropertyChanged();
        }
    }
}