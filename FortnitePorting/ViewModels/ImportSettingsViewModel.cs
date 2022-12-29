using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports.Blender;

namespace FortnitePorting.ViewModels;

public class ImportSettingsViewModel : ObservableObject
{
    public bool CanChangeRigOptions => BlenderRigType == ERigType.Default;
    public ERigType BlenderRigType
    {
        get => AppSettings.Current.BlenderExportSettings.RigType;
        set
        {
            AppSettings.Current.BlenderExportSettings.RigType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanChangeRigOptions));
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
    
    public bool BlenderUpdateTimeline
    {
        get => AppSettings.Current.BlenderExportSettings.UpdateTimeline;
        set
        {
            AppSettings.Current.BlenderExportSettings.UpdateTimeline = value;
            OnPropertyChanged();
        }
    }
    
    public bool BlenderLobbyPoses
    {
        get => AppSettings.Current.BlenderExportSettings.LobbyPoses;
        set
        {
            AppSettings.Current.BlenderExportSettings.LobbyPoses = value;
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
    
    public int BlenderLevelOfDetail
    {
        get => AppSettings.Current.BlenderExportSettings.LevelOfDetail;
        set
        {
            AppSettings.Current.BlenderExportSettings.LevelOfDetail = value;
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
    
    public bool BlenderImportMaterials
    {
        get => AppSettings.Current.BlenderExportSettings.ImportMaterials;
        set
        {
            AppSettings.Current.BlenderExportSettings.ImportMaterials = value;
            OnPropertyChanged();
        }
    }
    
    public double BlenderAmbientOcclusion
    {
        get => AppSettings.Current.BlenderExportSettings.AmbientOcclusion;
        set
        {
            AppSettings.Current.BlenderExportSettings.AmbientOcclusion = value;
            OnPropertyChanged();
        }
    }
    
    public double BlenderCavity
    {
        get => AppSettings.Current.BlenderExportSettings.Cavity;
        set
        {
            AppSettings.Current.BlenderExportSettings.Cavity = value;
            OnPropertyChanged();
        }
    }
    
    public double BlenderSubsurf
    {
        get => AppSettings.Current.BlenderExportSettings.Subsurface;
        set
        {
            AppSettings.Current.BlenderExportSettings.Subsurface = value;
            OnPropertyChanged();
        }
    }
}