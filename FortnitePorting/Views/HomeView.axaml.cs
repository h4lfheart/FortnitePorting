using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class HomeView : ViewBase<HomeViewModel>
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void OnNewsPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not NewsResponse news) return;
        
        Info.Dialog($"{news.Title}: {news.SubTitle}", news.Description);
    }

    private void OnFeaturedPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not FeaturedArtResponse featured) return;
        
        App.Launch(featured.Social);
    }
}