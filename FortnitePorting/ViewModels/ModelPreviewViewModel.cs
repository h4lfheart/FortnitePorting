using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.OpenGL;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.ViewModels;

public partial class ModelPreviewViewModel : ViewModelBase
{
    [ObservableProperty] private string _meshName;
    [ObservableProperty] private ModelViewerTkOpenGlControl _viewerControl;
}