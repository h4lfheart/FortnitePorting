using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Shared.Framework;

public class ViewModelBase : ObservableValidator
{
    public virtual async Task Initialize() { }  
    public virtual async Task OnViewOpened() { }  
    public virtual void OnApplicationExit() { }
}