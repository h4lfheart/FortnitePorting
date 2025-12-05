using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;

namespace FortnitePorting.ViewModels;

public partial class NewsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<NewsResponse> _news = [];

    public override async Task OnViewOpened()
    {
        News = [..await Api.FortnitePorting.News()];
    }
    
    public void OpenNews(NewsResponse news)
    {
        Info.Dialog($"{news.Title}: {news.SubTitle}", news.Description);
    }
}