using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Rendering;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.WindowModels;

public partial class ModelPreviewWindowModel : WindowModelBase
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