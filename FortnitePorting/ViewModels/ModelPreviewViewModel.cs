using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.OpenGL;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Windows;

namespace FortnitePorting.ViewModels;

public partial class ModelPreviewViewModel : WindowModelBase
{
    [ObservableProperty] private string _meshName;
    [ObservableProperty] private ModelPreviewControl _viewerControl;
    [ObservableProperty] private UObject _queuedObject;

    public override async Task Initialize()
    {
        await TaskService.RunDispatcherAsync(() =>
        {
            ViewerControl = new ModelPreviewControl();
            ViewerControl.Context.QueuedObject = QueuedObject;
        });
    }
}