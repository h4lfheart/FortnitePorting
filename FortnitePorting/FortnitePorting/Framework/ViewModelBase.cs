using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Framework;

public class ViewModelBase : ObservableObject
{
    public virtual async Task Initialize() { } 
}
