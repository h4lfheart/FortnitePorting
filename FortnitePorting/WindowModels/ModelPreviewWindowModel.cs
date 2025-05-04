using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Framework;
using FortnitePorting.Rendering;
using FortnitePorting.Services;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.WindowModels;

public partial class ModelPreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    
    [ObservableProperty] private string _meshName;
    [ObservableProperty] private ModelPreviewControl _viewerControl;
    [ObservableProperty] private Queue<UObject> _queuedObjects = [];
    [ObservableProperty] private bool _isLoading = false;

    public override async Task Initialize()
    {
        await TaskService.RunDispatcherAsync(() =>
        {
            ViewerControl = new ModelPreviewControl();
        });
        
        LoadQueue(QueuedObjects);
    }

    public void LoadQueue(Queue<UObject> queue)
    {
        ViewerControl.Context.Renderer?.Clear();
        ViewerControl.Context.ModelQueue = queue;
        
        TaskService.Run(() =>
        {
            IsLoading = true;
            while (ViewerControl.Context.LoadingModelQueue) { }
            IsLoading = false;
        });
    }
}