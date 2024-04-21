using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Framework;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusText = string.Empty;
    
    public override async Task Initialize()
    {
        Log.Information("Home Init");
    }
    
    public void UpdateStatus(string text)
    {
        StatusText = text;
    }
}