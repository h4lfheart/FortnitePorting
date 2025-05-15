using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;

namespace FortnitePorting.Models.Information;
public partial class MessageData : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _message;
    [ObservableProperty] private InfoBarSeverity _severity;
    [ObservableProperty] private bool _autoClose;
    [ObservableProperty] private string _id;
    [ObservableProperty] private float _closeTime;

    [ObservableProperty] private bool _useButton;
    [ObservableProperty] private string _buttonTitle;
    [ObservableProperty] private RelayCommand _buttonCommand;

    public MessageData(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 2.0f,
        bool useButton = false, string buttonTitle = "", Action? buttonCommand = null)
    {
        Title = title;
        Message = message;
        Severity = severity;
        AutoClose = autoClose;
        Id = id;
        CloseTime = closeTime;
        UseButton = useButton;
        ButtonTitle = buttonTitle;
        ButtonCommand = new RelayCommand(buttonCommand ?? (() => { }));
    }
}