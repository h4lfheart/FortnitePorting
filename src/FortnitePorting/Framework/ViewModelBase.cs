using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FortnitePorting.Framework;

public class ViewModelBase : ObservableValidator
{
    [JsonIgnore] public bool IsInitialized { get; set; }

    public void InvalidateInitialization() => IsInitialized = false;

    public virtual async Task Initialize() { }
    public virtual async Task OnViewOpened() { }
    public virtual async Task OnViewExited() { }
    public virtual void OnApplicationExit() { }
}
