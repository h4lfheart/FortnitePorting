using System;
using System.Windows;
using AdonisUI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace FortnitePorting.ViewModels;

public class ApplicationViewModel : ObservableObject
{
    public MainViewModel MainVM;
    public StartupViewModel StartupVM;
    public SettingsViewModel SettingsVM;
    public CUE4ParseViewModel CUE4ParseVM;
    public AssetHandlerViewModel? AssetHandlerVM;
    public ImportSettingsViewModel? ImportSettingsVM;

    public void Warning(string caption, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var messageBox = new MessageBoxModel
            {
                Caption = caption,
                Icon = MessageBoxImage.Warning,
                Text = message,
                Buttons = new[] { MessageBoxButtons.Ok() }
            };
        
            //AppLog.Warning($"{caption}: {message}");
            MessageBox.Show(messageBox);
        });
      
    }

    public void RestartWithMessage(string caption, string message)
    {
        var messageBox = new MessageBoxModel
        {
            Caption = caption,
            Icon = MessageBoxImage.Exclamation,
            Text = message,
            Buttons = new[] { MessageBoxButtons.Ok() }
        };

        MessageBox.Show(messageBox);
        Restart();
    }

    public void Restart()
    {
        AppHelper.Launch(AppDomain.CurrentDomain.FriendlyName, shellExecute: false);
        Application.Current.Shutdown();
    }

    public void Quit()
    {
        AppSettings.Save();
        Application.Current.Shutdown();
    }
}