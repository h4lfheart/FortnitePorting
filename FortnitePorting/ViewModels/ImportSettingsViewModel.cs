using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports.Blender;

namespace FortnitePorting.ViewModels;

public class ImportSettingsViewModel : ObservableObject
{
    public ERigType BlenderRigType
    {
        get => AppSettings.Current.BlenderExportSettings.RigType;
        set
        {
            AppSettings.Current.BlenderExportSettings.RigType = value;
            OnPropertyChanged();
        }
    }
    
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
    
    public bool BlenderQuadTopo
    {
        get => AppSettings.Current.BlenderExportSettings.QuadTopo;
        set
        {
            AppSettings.Current.BlenderExportSettings.QuadTopo = value;
            OnPropertyChanged();
        }
    }
    
    public bool BlenderPoseFixes
    {
        get => AppSettings.Current.BlenderExportSettings.PoseFixes;
        set
        {
            AppSettings.Current.BlenderExportSettings.PoseFixes = value;
            OnPropertyChanged();
        }
    }
    
    public float BlenderAmbientOcclusion
    {
        get => AppSettings.Current.BlenderExportSettings.AmbientOcclusion;
        set
        {
            AppSettings.Current.BlenderExportSettings.AmbientOcclusion = value;
            OnPropertyChanged();
        }
    }
    
    public float BlenderCavity
    {
        get => AppSettings.Current.BlenderExportSettings.Cavity;
        set
        {
            AppSettings.Current.BlenderExportSettings.Cavity = value;
            OnPropertyChanged();
        }
    }
    
    public float BlenderSubsurf
    {
        get => AppSettings.Current.BlenderExportSettings.Subsurface;
        set
        {
            AppSettings.Current.BlenderExportSettings.Subsurface = value;
            OnPropertyChanged();
        }
    }
}