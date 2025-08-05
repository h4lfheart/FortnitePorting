using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Rendering;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class ModelPreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    
    [ObservableProperty] private string _meshName;
    [ObservableProperty] private ModelPreviewControl _control;
    [ObservableProperty] private Queue<UObject> _queuedObjects = [];
    [ObservableProperty] private bool _isLoading = false;
    
    [ObservableProperty] private static ModelViewerContext? _context;

    public void LoadQueue(Queue<UObject> queue)
    {
        Context.Renderer?.Clear();
        Context.ModelQueue = queue;
        
        TaskService.Run(() =>
        {
            IsLoading = true;
            while (Context.LoadingModelQueue) { }
            IsLoading = false;
        });
    }

    public override void OnApplicationExit()
    {
        base.OnApplicationExit();
        
        Context?.Close();
    }
}