using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.App;

public partial class TitleData : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _subTitle;

    public TitleData(string title, string subTitle)
    {
        Title = title;
        SubTitle = subTitle;
    }
}