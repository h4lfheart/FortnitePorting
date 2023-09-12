using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private UserControl currentPage;
    
    public void SetPage<T>() where T : UserControl, new()
    {
        CurrentPage = new T();
    }
}