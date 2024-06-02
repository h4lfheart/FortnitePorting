using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Shared.Framework;

public class ViewModelBase : ObservableObject
{
    public virtual async Task Initialize() { }  
    public virtual void OnApplicationExit() { }
}