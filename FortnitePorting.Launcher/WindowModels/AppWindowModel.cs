using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models.App;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Launcher.WindowModels;

public partial class AppWindowModel : ViewModelBase
{ 
    public Frame ContentFrame;
    public NavigationView NavigationView;

    [ObservableProperty] private bool _finishedSetup;
    
    [ObservableProperty] private ObservableCollection<InfoBarData> _infoBars = [];
    
    public void Message(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 3f, bool useButton = false, string buttonTitle = "", Action? buttonCommand = null)
    {
        Message(new InfoBarData(title, message, severity, autoClose, id, closeTime, useButton, buttonTitle, buttonCommand));
    }

    public void Message(InfoBarData data)
    {
        if (!string.IsNullOrEmpty(data.Id))
            InfoBars.RemoveAll(bar => bar.Id.Equals(data.Id));
        
        InfoBars.Add(data);
        if (!data.AutoClose) return;
        
        TaskService.Run((Func<Task>)(async () =>
        {
            await Task.Delay((int) (data.CloseTime * 1000));
            InfoBars.Remove(data);
        }));
    }
    
    public void UpdateMessage(string id, string message)
    {
        var foundInfoBar = InfoBars.FirstOrDefault(infoBar => infoBar.Id == id);
        if (foundInfoBar is null) return;
        
        foundInfoBar.Message = message;
    }
    
    public void CloseMessage(string id)
    {
        InfoBars.RemoveAll(info => info.Id == id);
    }
    
    public void Dialog(string title, string content)
    {
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Continue"
            };
            
            await dialog.ShowAsync();
        });
    }
    
    public void Navigate<T>()
    {
        Navigate(typeof(T));
    }
    
    public void Navigate(Type type)
    {
        ContentFrame.Navigate(type, null, null);

        var buttonName = type.Name.Replace("View", string.Empty);
        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (string) item.Tag! == buttonName);
    }

    public bool IsInView<T>()
    {
        var result = false;
        TaskService.RunDispatcher(() =>
        {
            result = AppWM.ContentFrame.CurrentSourcePageType == typeof(T);
        });
        return result;
    }

    public void ToggleVisibility(bool vis)
    {
        ContentFrame.IsVisible = vis;
    }
}