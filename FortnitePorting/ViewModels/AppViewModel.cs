using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Application;
using FortnitePorting.Models;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.ViewModels;

public partial class AppViewModel : WindowModelBase
{
    [ObservableProperty] private string _versionString = Globals.VersionString;
    [ObservableProperty] private bool _gameBasedTabsAreReady = false;
    [ObservableProperty] private bool _setupTabsAreVisible = true;
    [ObservableProperty] private Frame _contentFrame;
    [ObservableProperty] private NavigationView _navigationView;
    [ObservableProperty] private ObservableCollection<InfoBarData> _infoBars = [];

    public override async Task Initialize()
    {
        SetupTabsAreVisible = !AppSettings.Current.FinishedWelcomeScreen;
    }
    
    public void Message(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 2.0f)
    {
        Message(new InfoBarData(title, message, severity, autoClose, id, closeTime));
    }

    public void Message(InfoBarData data)
    {
        InfoBars.Add(data);
        if (!data.AutoClose) return;
        
        TaskService.Run(async () =>
        {
            await Task.Delay((int) (data.CloseTime * 1000));
            InfoBars.Remove(data);
        });
    }
    
    public void UpdateMessage(string id, string message)
    {
        InfoBars.FirstOrDefault(infoBar => infoBar.Id == id)!.Message = message;
    }
    
    public void CloseMessage(string id)
    {
        InfoBars.RemoveAll(info => info.Id == id);
    }

    public void Navigate<T>()
    {
        Navigate(typeof(T));
    }
    
    public void Navigate(Type type)
    {
        ContentFrame.Navigate(type, null, AppSettings.Current.Application.Transition);

        var buttonName = type.Name.Replace("View", string.Empty);
        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (string) item.Tag! == buttonName);
    }
}