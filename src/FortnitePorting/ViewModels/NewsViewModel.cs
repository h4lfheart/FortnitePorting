using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Windows;

namespace FortnitePorting.ViewModels;

public partial class NewsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<NewsEntry> _news = [];

    public override async Task OnViewOpened()
    {
        var newsResponse = await Api.FortnitePorting.News();
        News = [..newsResponse.Entries];
    }
    
    public void OpenNews(NewsEntry news)
    {
        ChangelogWindow.Preview(news.Description);
    }
}