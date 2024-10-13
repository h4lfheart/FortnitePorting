using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Export;

namespace FortnitePorting.ViewModels.Settings;

public partial class UnrealSettingsViewModel : BaseExportSettings
{
    // Skeleton
    [ObservableProperty] private bool _importSockets = true;
    [ObservableProperty] private bool _importVirtualBones = false;
    
    // Mesh
    [ObservableProperty] private bool _importCollision = false;
    
    // Material
    [ObservableProperty] private bool _useUEFNMaterial = false;
    [ObservableProperty] private float _ambientOcclusion = 0.0f;
    [ObservableProperty] private float _cavity = 0.0f;
    [ObservableProperty] private float _subsurface = 0.0f;
    
    // Sound
    [ObservableProperty] private bool _importSounds = false;
}