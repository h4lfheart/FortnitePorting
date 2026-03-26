using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Windows;

namespace FortnitePorting.ViewModels;

public partial class NewsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<NewsResponse> _news = [];

    public override async Task OnViewOpened()
    {
        News = [..await Api.FortnitePorting.News()];

        AppWM.UpdateChippy([
            "any good news?", "maybe one day my face can be on a news article :(", "i love staying informed!! kinda!!",
            "breaking news: you’re awesome", "i’m your tiny news anchor 😺"
        ]);
    }

    public void OpenNews(NewsResponse news)
    {
        ChangelogWindow.Preview(news.Description);
    }
}