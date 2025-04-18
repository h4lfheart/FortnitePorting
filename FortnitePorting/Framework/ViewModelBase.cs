using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Framework;

public class ViewModelBase : ObservableValidator
{
    public virtual async Task Initialize() { }  
    public virtual async Task OnViewOpened() { }  
    public virtual async Task OnViewExited() { }  
    public virtual void OnApplicationExit() { }
}